using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MLoop.Services;

namespace MLoop.BackgroundServices;

public class MLoopBackgroundService : BackgroundService
{
    private readonly ILogger<MLoopBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private MLoopTrainService _trainService;
    private MLoopPredictService _predictService;

    public MLoopBackgroundService(ILogger<MLoopBackgroundService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MLoopBackgroundService is starting.");

        _trainService = _serviceProvider.GetRequiredService<MLoopTrainService>();
        _predictService = _serviceProvider.GetRequiredService<MLoopPredictService>();

        await Task.WhenAll(
            _trainService.StartAsync(stoppingToken),
            _predictService.StartAsync(stoppingToken)
        );
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MLoopBackgroundService is stopping.");

        await Task.WhenAll(
            _trainService.StopAsync(stoppingToken),
            _predictService.StopAsync(stoppingToken)
        );

        await base.StopAsync(stoppingToken);
    }
}