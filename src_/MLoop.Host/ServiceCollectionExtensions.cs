using Microsoft.Extensions.Configuration;
using MLoop;
using MLoop.Actions;
using MLoop.BackgroundServices;
using MLoop.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfiguresMLoop(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<MLTrainService>();
            services.AddTransient<MLTrainActionExecutor>();

            services.Configure<MLoopOptions>(configuration.GetSection("MLoop"));
            services.AddSingleton<MLoopApiService>();

            services.AddHostedService<MLoopBackgroundService>();
            services.AddSingleton<MLoopTrainService>();
            services.AddSingleton<MLoopPredictService>();
        }
    }
}
