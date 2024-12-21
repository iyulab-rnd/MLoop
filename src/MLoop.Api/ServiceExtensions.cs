using Microsoft.Extensions.Options;
using MLoop.Storages.Configuration;
using MLoop.Storages;

namespace MLoop.Api;

public static class ServiceExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));

        services.AddSingleton<IFileStorage>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<StorageSettings>>();
            var basePath = settings.Value.BasePath;

            // 상대 경로인 경우 애플리케이션 기준으로 절대 경로 변환
            if (!Path.IsPathRooted(basePath))
            {
                basePath = Path.Combine(Directory.GetCurrentDirectory(), basePath);
            }

            return new LocalFileStorage(basePath);
        });

        return services;
    }
}