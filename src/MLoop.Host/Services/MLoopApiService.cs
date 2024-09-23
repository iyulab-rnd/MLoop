using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Actions;
using MLoop.Internal;
using MLoop.Models;
using MLoop.Utils;

namespace MLoop.Services;

public class MLoopApiService
{
    private readonly ILogger<MLoopApiService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MLoopOptions _options;

    public MLoopApiService(ILogger<MLoopApiService> logger,
        IServiceProvider serviceProvider,
        IOptions<MLoopOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public async IAsyncEnumerable<MLScenario> GetMLScenariosAsync()
    {
        var basePath = Path.GetFullPath(_options.Path);
        var directories = Directory.GetDirectories(basePath);

        foreach (var directory in directories)
        {
            MLScenario? scenario = null;
            try
            {
                scenario = await MLoopFactory.GetMLScenarioAsync(directory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scenario from directory {Directory}", directory);
            }
            if (scenario == null) continue;

            yield return scenario;
        }
    }

    public async Task<MLScenario?> GetMLScenarioAsync(string scenarioName)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var scenarioPath = Path.Combine(basePath, scenarioName);

        if (!Directory.Exists(scenarioPath))
        {
            return null;
        }

        return await MLoopFactory.GetMLScenarioAsync(scenarioPath);
    }

    public async Task<MLTrainActionResult> BuildNewModelAsync(MLScenario scenario, TrainOptions? trainOptions = null)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var modelKey = RandomHelper.RandomString(8);
        var modelPath = Path.Combine(basePath, scenario.Name, modelKey);
        
        if (scenario.DataPath == null || !File.Exists(scenario.DataPath)) 
            throw new InvalidDataException($"cannot find data, scenario: {scenario.Name}");

        var action = await MLoopFactory.BuildDefaultActionAsync(modelPath, trainOptions);

        var executor = _serviceProvider.GetRequiredService<MLTrainActionExecutor>();
        return await executor.ExecuteAsync(action);
    }

    public async IAsyncEnumerable<MLModel> GetModelsAsync(MLScenario scenario)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var scenarioPath = Path.Combine(basePath, scenario.Name);
        var directories = Directory.GetDirectories(scenarioPath);

        foreach (var directory in directories)
        {
            MLModel? model = null;

            try
            {
                var modelName = Path.GetFileName(directory);
                model = await MLoopFactory.CreateMLModelAsync(modelName, directory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating model from directory {Directory}", directory);
            }
            if (model == null) continue;
            yield return model;
        }
    }

    public Task<MLModel> GetMLModelAsync(MLScenario scenario, string modelName)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var modelPath = Path.Combine(basePath, scenario.Name, modelName);
        if (!Directory.Exists(modelPath))
        {
            throw new EntryPointNotFoundException($"Model not found. {modelName}");
        }

        return MLoopFactory.CreateMLModelAsync(modelName, modelPath);
    }

    public async Task<string> GetMLModelLogAsync(MLScenario scenario, string modelName)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var modelPath = Path.Combine(basePath, scenario.Name, modelName);
        if (!Directory.Exists(modelPath))
        {
            throw new EntryPointNotFoundException($"Model not found. {modelName}");
        }

        var logPath = Path.Combine(modelPath, "console.log");
        if (!File.Exists(logPath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(logPath);
    }

    internal async Task<MLModel?> GetPredictionModelAsync(MLScenario scenario)
    {
        MLModel? lastModel = null;

        await foreach(var model in GetModelsAsync(scenario))
        {
            if (model.TrainState == null || model.TrainState.TrainingCompleted == null) continue;

            if (lastModel == null || model.TrainState.TrainingCompleted > lastModel.TrainState!.TrainingCompleted)
            {
                lastModel = model;
            }
        }

        return lastModel;
    }
}