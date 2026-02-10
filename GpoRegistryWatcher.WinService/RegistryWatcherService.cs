using System.Management;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace GpoRegistryWatcher.WinService
{
    public class RegistryWatcherService : BackgroundService
    {
        private const string RegPath = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";

        private static readonly Dictionary<string, int> s_desiredValues = new()
        {
            ["EnableLUA"] = 1,
            ["DontDisplayLastUsername"] = 0
        };

        private readonly List<ManagementEventWatcher> _watchers = [];
        private static readonly Dictionary<string, DateTime> s_lastWrite = [];
        private static readonly TimeSpan s_cooldown = TimeSpan.FromSeconds(5);

        private readonly ILogger<RegistryWatcherService> _logger;

        public RegistryWatcherService(ILogger<RegistryWatcherService> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Watching registry changes...");

            FixCurrentValues();

            StartWatcher("EnableLUA");
            StartWatcher("DontDisplayLastUsername");

            // Vänta tills tjänsten stoppas
            stoppingToken.Register(StopWatchers);

            return Task.CompletedTask;
        }

        private void StartWatcher(string valueName)
        {
            var query = new WqlEventQuery($@"
                SELECT * FROM RegistryValueChangeEvent
                WHERE Hive='HKEY_LOCAL_MACHINE'
                AND KeyPath='{RegPath.Replace("\\", "\\\\")}'
                AND ValueName='{valueName}'
            ");

            var watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += OnEventArrived;

            watcher.Start();
            _watchers.Add(watcher);
            _logger.LogInformation("Started watcher for {ValueName}", valueName);
        }

        private void StopWatchers()
        {
            _logger.LogInformation("Stopping registry watchers...");

            foreach (var watcher in _watchers)
            {
                try
                {
                    watcher.EventArrived -= OnEventArrived;
                    watcher.Stop();
                    watcher.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop watcher");
                }
            }

            _watchers.Clear();
        }

        private void OnEventArrived(object sender, EventArrivedEventArgs e)
        {
            var valueName = (string)e.NewEvent["ValueName"];

            var current = ReadDword(valueName);
            var desired = s_desiredValues[valueName];

            _logger.LogInformation("{ValueName} changed → {Current}", valueName, current);

            if (current != desired)
            {
                if (s_lastWrite.TryGetValue(valueName, out var last) &&
                    DateTime.Now - last < s_cooldown)
                {
                    _logger.LogInformation("  Skipping revert (cooldown)");
                    return;
                }

                _logger.LogInformation("  Reverting {ValueName} → {Desired}", valueName, desired);

                SetDword(valueName, desired);
                s_lastWrite[valueName] = DateTime.Now;
            }
        }

        private static int ReadDword(string valueName)
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegPath);
            return key?.GetValue(valueName) as int? ?? -1;
        }

        private static void SetDword(string valueName, int value)
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegPath, writable: true);
            key?.SetValue(valueName, value, RegistryValueKind.DWord);
        }

        private void FixCurrentValues()
        {
            foreach ((var valueName, var desired) in s_desiredValues)
            {
                var current = ReadDword(valueName);
                _logger.LogInformation("{ValueName} = {Current}", valueName, current);

                if (current != desired)
                {
                    _logger.LogInformation("  Fixing {ValueName} → {Desired}", valueName, desired);

                    SetDword(valueName, desired);
                    s_lastWrite[valueName] = DateTime.Now;
                }
            }
        }
    }
}

