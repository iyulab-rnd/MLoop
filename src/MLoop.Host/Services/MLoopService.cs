using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using MLoop.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MLoop.Services
{
    public class MLoopService : BackgroundService
    {
        private readonly ILogger<MLoopService> _logger;
        private readonly Channel<BuildModelAction> _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly MLoopOptions _options;

        public MLoopService(ILogger<MLoopService> logger, Channel<BuildModelAction> channel, IServiceProvider serviceProvider, IOptions<MLoopOptions> options)
        {
            _logger = logger;
            _channel = channel;
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MLoopService is starting.");

            stoppingToken.Register(() =>
                _logger.LogInformation("MLoopService is stopping."));

            var executors = Enumerable.Range(0, _options.Threads)
                                      .Select(_ => _serviceProvider.GetRequiredService<BuildModelActionExecutor>())
                                      .ToArray();

            var tasks = executors.Select(executor => ProcessQueueAsync(executor, stoppingToken)).ToArray();

            await Task.WhenAll(tasks);
        }

        private async Task ProcessQueueAsync(BuildModelActionExecutor executor, CancellationToken stoppingToken)
        {
            await foreach (var action in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var result = await executor.ExecuteAsync(action, stoppingToken);
                    if (result.Success)
                    {
                        _logger.LogInformation($"Successfully processed action from {action.InputPath} to {action.OutputPath}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to process action from {action.InputPath} to {action.OutputPath}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing action from {action.InputPath} to {action.OutputPath}");
                }
            }
        }
    }
}
