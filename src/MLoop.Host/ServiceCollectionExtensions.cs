using Microsoft.Extensions.Hosting;
using MLoop;
using MLoop.Actions;
using MLoop.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfiguresMLoop(this IServiceCollection services, HostBuilderContext context)
        {
            services.AddSingleton<MLTrainService>();
            services.AddTransient<MLTrainActionExecutor>();

            services.Configure<MLoopOptions>(context.Configuration.GetSection("MLoop"));
            services.AddHostedService<MLoopService>();
        }
    }
}
