using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace GpoRegistryWatcher.ConsoleApp
{
    internal partial class Program
    {
        private const string RegPath = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";

        private static readonly Dictionary<string, int> s_desiredValues = new()
        {
            ["EnableLUA"] = 1,
            ["DontDisplayLastUsername"] = 0
        };

        private static readonly Dictionary<string, DateTime> s_lastWrite = [];
        private static readonly TimeSpan s_cooldown = TimeSpan.FromSeconds(5);

        static void Main()
        {
            EnsureConsole();

            Console.WriteLine("Watching registry changes...");
            FixCurrentValues();

            StartWatcher("EnableLUA");
            StartWatcher("DontDisplayLastUsername");

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }

        static void StartWatcher(string valueName)
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
        }

        static void OnEventArrived(object sender, EventArrivedEventArgs e)
        {
            var valueName = (string)e.NewEvent["ValueName"];

            var current = ReadDword(valueName);
            var desired = s_desiredValues[valueName];

            Console.WriteLine($"{DateTime.Now:g} {valueName} changed → {current}");

            if (current != desired)
            {
                if (s_lastWrite.TryGetValue(valueName, out var last) &&
                    DateTime.Now - last < s_cooldown)
                {
                    Console.WriteLine("  Skipping revert (cooldown)");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Reverting {valueName} → {desired}");
                Console.ResetColor();

                SetDword(valueName, desired);
                s_lastWrite[valueName] = DateTime.Now;
            }
        }

        static int ReadDword(string valueName)
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegPath);
            return key?.GetValue(valueName) as int? ?? -1;
        }

        static void SetDword(string valueName, int value)
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegPath, writable: true);
            key?.SetValue(valueName, value, RegistryValueKind.DWord);
        }

        static void FixCurrentValues()
        {
            foreach ((var valueName, var desired) in s_desiredValues)
            {
                var current = ReadDword(valueName);
                Console.WriteLine($"{valueName} = {current}");

                if (current != desired)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Fixing {valueName} → {desired}");
                    Console.ResetColor();

                    SetDword(valueName, desired);
                    s_lastWrite[valueName] = DateTime.Now;
                }
            }
        }

        static void EnsureConsole()
        {
            const int ATTACH_PARENT_PROCESS = -1;

            if (!AttachConsole(ATTACH_PARENT_PROCESS))
            {
                AllocConsole();
            }
        }

#pragma warning disable SYSLIB1054

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();
    }
}