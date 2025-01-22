using Microsoft.Extensions.Logging;

namespace MLoop.Services;

public class BestModelManager
{
    private readonly ScenarioManager _scenarioManager;
    private readonly ILogger<BestModelManager> _logger;
    private readonly IFileStorage _storage;

    public BestModelManager(
        ScenarioManager scenarioManager,
        IFileStorage storage,
        ILogger<BestModelManager> logger)
    {
        _scenarioManager = scenarioManager;
        _storage = storage;
        _logger = logger;
    }

    public async Task UpdateBestModelIfBetterAsync(string scenarioId, string modelId)
    {
        try
        {
            var scenario = await _scenarioManager.LoadAsync(scenarioId);
            if (scenario == null)
            {
                _logger.LogError("Could not load scenario {ScenarioId}", scenarioId);
                return;
            }

            if (string.IsNullOrEmpty(modelId))
            {
                _logger.LogError("ModelId is null");
                return;
            }

            // Load current model
            var currentModel = await LoadModelMetadataAsync(scenarioId, modelId);
            if (currentModel?.Metrics == null || !currentModel.Metrics.ContainsKey("BestScore"))
            {
                _logger.LogWarning("Current model has no metrics");
                return;
            }

            // If there's no best model yet, set this as the best
            if (string.IsNullOrEmpty(scenario.BestModelId))
            {
                await SetNewBestModelAsync(scenario, modelId);
                return;
            }

            // Load previous best model
            var bestModel = await LoadModelMetadataAsync(scenarioId, scenario.BestModelId);
            if (bestModel?.Metrics == null || !bestModel.Metrics.ContainsKey("BestScore"))
            {
                _logger.LogWarning("Previous best model has no metrics");
                await SetNewBestModelAsync(scenario, modelId);
                return;
            }

            // Compare metrics based on ML type
            var isCurrentBetter = IsCurrentModelBetter(
                currentModel.Metrics,
                bestModel.Metrics,
                bestModel.MLType);

            if (isCurrentBetter)
            {
                await SetNewBestModelAsync(scenario, modelId);
                _logger.LogInformation(
                    "Updated best model for scenario {ScenarioId} from {OldModelId} to {NewModelId}",
                    scenarioId,
                    bestModel.ModelId,
                    modelId);
            }
            else
            {
                _logger.LogInformation(
                    "Kept existing best model {ModelId} for scenario {ScenarioId}",
                    bestModel.ModelId,
                    scenarioId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating best model for scenario {ScenarioId}",
                scenarioId);
        }
    }

    private async Task<MLModel?> LoadModelMetadataAsync(string scenarioId, string modelId)
    {
        var modelMetadataPath = Path.Combine(
            _storage.GetModelPath(scenarioId, modelId),
            "model.json");

        if (!File.Exists(modelMetadataPath))
        {
            _logger.LogWarning("Model metadata not found at {Path}", modelMetadataPath);
            return null;
        }

        try
        {
            var modelJson = await File.ReadAllTextAsync(modelMetadataPath);
            return JsonHelper.Deserialize<MLModel>(modelJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading model metadata for {ModelId}", modelId);
            return null;
        }
    }

    private async Task SetNewBestModelAsync(MLScenario scenario, string modelId)
    {
        scenario.BestModelId = modelId;
        await _scenarioManager.SaveAsync(scenario.ScenarioId, scenario);
        _logger.LogInformation(
            "Set new best model for scenario {ScenarioId}: {ModelId}",
            scenario.ScenarioId,
            modelId);
    }

    private bool IsCurrentModelBetter(
        MLModelMetrics currentMetrics,
        MLModelMetrics previousMetrics,
        string mlType)
    {
        // Both metrics should have BestScore
        if (!currentMetrics.ContainsKey("BestScore") ||
            !previousMetrics.ContainsKey("BestScore"))
        {
            return false;
        }

        var currentScore = currentMetrics["BestScore"];
        var previousScore = previousMetrics["BestScore"];

        return mlType.ToLowerInvariant() switch
        {
            // 분류 작업: 높은 정확도가 더 좋음
            var t when t.Contains("classification") => currentScore > previousScore,

            // 회귀 작업: 낮은 오차가 더 좋음 (MSE, MAE 등)
            var t when t.Contains("regression") => currentScore < previousScore,

            // 추천 작업: 높은 NDCG 또는 MAP가 더 좋음
            var t when t.Contains("recommendation") => currentScore > previousScore,

            // 예측 작업: 낮은 오차 지표가 더 좋음
            var t when t.Contains("forecasting") => currentScore < previousScore,

            // 기본적으로는 높은 점수가 더 좋다고 가정
            _ => currentScore > previousScore
        };
    }
}