using Microsoft.Extensions.Configuration;
using MLoop;
using MLoop.Actions;
using MLoop.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfiguresMLoop(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IMLTrainService, LocalMLTrainService>();
            services.AddTransient<MLTrainActionExecutor>();

            services.Configure<MLoopOptions>(configuration.GetSection("MLoop"));
            services.AddHostedService<MLoopService>();
        }
    }
}
