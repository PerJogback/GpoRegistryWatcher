using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GpoRegistryWatcher.WinService
{
    internal partial class Program
    {


        static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices(services =>
            {
                services.AddHostedService<RegistryWatcherService>();
            })
            .Build()
            .Run();
        }
    }
}