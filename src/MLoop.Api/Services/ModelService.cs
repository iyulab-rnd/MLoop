using MLoop.Models;
using MLoop.Storages;

namespace MLoop.Api.Services;

public class ModelService
{
    private readonly IFileStorage _storage;
    private readonly ILogger<ModelService> _logger;

    public ModelService(IFileStorage storage, ILogger<ModelService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<IEnumerable<MLModel>> GetModelsAsync(string scenarioId)
    {
        var models = new List<MLModel>();
        var modelsDir = _storage.GetScenarioModelsDir(scenarioId);

        if (!Directory.Exists(modelsDir))
            return models;

        foreach (var modelDir in Directory.GetDirectories(modelsDir))
        {
            var modelId = Path.GetFileName(modelDir);
            try
            {
                var metadata = await GetModelAsync(scenarioId, modelId);
                if (metadata != null)
                {
                    models.Add(metadata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading model {ModelId} in scenario {ScenarioId}", modelId, scenarioId);
            }
        }

        return models;
    }

    public async Task<MLModel?> GetModelAsync(string scenarioId, string modelId)
    {
        var metadataPath = Path.Combine(_storage.GetModelPath(scenarioId, modelId), "metadata.json");
        if (!File.Exists(metadataPath))
            return null;

        var json = await File.ReadAllTextAsync(metadataPath);
        var model = JsonHelper.Deserialize<MLModel>(json);
        return model;
    }

    public async Task<string?> GetModelTrainLogsAsync(string scenarioId, string modelId)
    {
        var logPath = Path.Combine(_storage.GetModelPath(scenarioId, modelId), "train.log");
        if (!File.Exists(logPath))
            return null;

        return await File.ReadAllTextAsync(logPath);
    }

    public async Task<MLModelMetrics?> GetModelMetricsAsync(string scenarioId, string modelId)
    {
        var metricsPath = Path.Combine(_storage.GetModelPath(scenarioId, modelId), "metrics.json");
        if (!File.Exists(metricsPath))
            return null;

        var json = await File.ReadAllTextAsync(metricsPath);
        return JsonHelper.Deserialize<MLModelMetrics>(json);
    }

    public async Task<MLModel?> GetBestModelAsync(string scenarioId)
    {
        var models = await GetModelsAsync(scenarioId);
        foreach(var model in models)
        {
            model.Metrics = await GetModelMetricsAsync(scenarioId, model.ModelId);
        }

        var bestModel = models.OrderByDescending(m => m.Metrics?.GetValueOrDefault("BestScore", 0.0))
                             .ThenByDescending(m => m.CreatedAt)
                             .FirstOrDefault();
        return bestModel;
    }
}