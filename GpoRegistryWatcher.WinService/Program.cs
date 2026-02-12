using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GpoRegistryWatcher.WinService
{
    internal partial class Program
    {


        static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddEventLog();
            })
            .ConfigureServices(services =>
            {
                services.AddHostedService<RegistryWatcherService>();
            })
            .Build()
            .Run();
        }
    }
}