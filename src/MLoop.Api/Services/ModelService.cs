using MLoop.Api.Models.Models;
using MLoop.Base;
using MLoop.Models;
using MLoop.Storages;

namespace MLoop.Api.Services;

public class ModelService : ScenarioServiceBase<MLModel, CreateModelRequest, UpdateModelRequest>
{
    private readonly IFileStorage _storage;
    private readonly ModelHandler _modelHandler;

    public ModelService(
        IFileStorage storage,
        ModelHandler modelHandler,
        ILogger<ModelService> logger) : base(logger)
    {
        _storage = storage;
        _modelHandler = modelHandler;
    }

    public override async Task<MLModel> CreateAsync(string scenarioId, CreateModelRequest request)
    {
        var model = new MLModel(
            request.ModelId,
            request.MLType,
            request.Command,
            request.Arguments ?? []
        )
        {
            ScenarioId = scenarioId
        };

        return await _modelHandler.ProcessAsync(model);
    }

    public override async Task<MLModel?> GetAsync(string scenarioId, string modelId)
    {
        var modelPath = Path.Combine(_storage.GetModelPath(scenarioId, modelId), "model.json");

        if (!File.Exists(modelPath))
            return null;

        var json = await File.ReadAllTextAsync(modelPath);
        return JsonHelper.Deserialize<MLModel>(json);
    }

    public override Task<MLModel> UpdateAsync(string scenarioId, string modelId, UpdateModelRequest request)
    {
        throw new NotImplementedException("Model updates are not supported");
    }

    public override Task DeleteAsync(string scenarioId, string modelId)
    {
        var modelPath = _storage.GetModelPath(scenarioId, modelId);

        if (Directory.Exists(modelPath))
            Directory.Delete(modelPath, true);
        return Task.CompletedTask;
    }

    public async Task<MLModel?> GetBestModelAsync(string scenarioId)
    {
        var models = await GetModelsAsync(scenarioId);
        return models.OrderByDescending(m => m.Metrics?.GetValueOrDefault("BestScore", 0))
                    .FirstOrDefault();
    }

    public async Task<IEnumerable<MLModel>> GetModelsAsync(string scenarioId)
    {
        var modelsDir = _storage.GetScenarioModelsDir(scenarioId);
        if (!Directory.Exists(modelsDir))
            return [];

        var models = new List<MLModel>();
        foreach (var modelDir in Directory.GetDirectories(modelsDir))
        {
            var modelId = Path.GetFileName(modelDir);
            var model = await GetAsync(scenarioId, modelId);
            if (model != null)
            {
                models.Add(model);
            }
        }

        return models;
    }

    public async Task<string?> GetModelTrainLogsAsync(string scenarioId, string modelId)
    {
        var logsPath = Path.Combine(_storage.GetModelPath(scenarioId, modelId), "train.log");
        if (!File.Exists(logsPath))
            return null;

        return await File.ReadAllTextAsync(logsPath);
    }

    public async Task<MLModelMetrics?> GetModelMetricsAsync(string scenarioId, string modelId)
    {
        var model = await GetAsync(scenarioId, modelId);
        return model?.Metrics;
    }

    public async Task<Dictionary<string, bool>> ValidateModelAsync(string scenarioId, string modelId)
    {
        _ = await GetAsync(scenarioId, modelId) ?? throw new ValidationException($"Model {modelId} not found");
        var modelPath = _storage.GetModelPath(scenarioId, modelId);
        var validation = new Dictionary<string, bool>
        {
            ["exists"] = Directory.Exists(modelPath),
            ["hasMetadata"] = File.Exists(Path.Combine(modelPath, "model.json")),
            ["hasModelFiles"] = Directory.GetFiles(modelPath).Length > 1
        };

        return validation;
    }
}