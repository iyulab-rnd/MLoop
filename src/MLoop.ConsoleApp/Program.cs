using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MLoop.Actions;
using MLoop.Contracts;
using MLoop.Services;
using System.Threading.Channels;

namespace MLoop.ConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<MLoopOptions>(context.Configuration.GetSection("MLoop"));

                    services.AddSingleton<IMLFileProvider, LocalFileProvider>();
                    services.AddSingleton<BuildModelActionExecutor>();

                    var channel = Channel.CreateUnbounded<BuildModelAction>();
                    services.AddSingleton(channel);

                    services.AddHostedService<MLoopService>();
                    services.AddHostedService<MLoopFileWatcher>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
