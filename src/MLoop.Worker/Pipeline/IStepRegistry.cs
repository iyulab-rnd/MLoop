using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MLoop.Worker.Tasks.MLNet.StepRunners;

namespace MLoop.Worker.Pipeline;

public interface IStepRegistry
{
    IStepRunner GetRunner(string type);
    bool HasRunner(string type);
    IEnumerable<string> GetSupportedTypes();
}

public class StepRegistry : IStepRegistry
{
    private readonly Dictionary<string, IStepRunner> _runners;
    private readonly ILogger<StepRegistry> _logger;

    public StepRegistry(
        IEnumerable<IStepRunner> runners,
        ILogger<StepRegistry> logger)
    {
        _logger = logger;
        _runners = new Dictionary<string, IStepRunner>(StringComparer.OrdinalIgnoreCase);

        foreach (var runner in runners)
        {
            try
            {
                if (_runners.ContainsKey(runner.Type))
                {
                    _logger.LogWarning(
                        "Duplicate step runner type found: {RunnerType}. Using the last registered runner.",
                        runner.Type);
                }
                _runners[runner.Type] = runner;
                _logger.LogInformation("Registered step runner: {RunnerType}", runner.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to register step runner of type {RunnerType}",
                    runner.GetType().Name);
            }
        }
    }

    public IStepRunner GetRunner(string type)
    {
        if (!_runners.TryGetValue(type, out var runner))
        {
            _logger.LogError("No runner found for step type: {StepType}", type);
            throw new InvalidOperationException($"No runner found for step type: {type}");
        }

        return runner;
    }

    public bool HasRunner(string type)
    {
        return _runners.ContainsKey(type);
    }

    public IEnumerable<string> GetSupportedTypes()
    {
        return _runners.Keys;
    }
}

public static class StepRegistryExtensions
{
    public static IServiceCollection AddStepRunners(this IServiceCollection services)
    {
        // Register all step runners
        services.AddSingleton<IStepRunner, MLNetTrainStepRunner>();
        services.AddSingleton<IStepRunner, MLNetPredictStepRunner>();

        // Register the registry itself
        services.AddSingleton<IStepRegistry, StepRegistry>();

        return services;
    }
}