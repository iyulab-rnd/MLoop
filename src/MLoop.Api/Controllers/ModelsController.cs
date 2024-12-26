using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Services;
using MLoop.Services;
using MLoop.Storages;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/models")]
public class ModelsController : ControllerBase
{
    private readonly ModelService _modelService;
    private readonly ScenarioManager _scenarioManager;
    private readonly JobService _jobService;
    private readonly IFileStorage _storage;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(
        ModelService modelService,
        ScenarioManager scenarioManager,
        JobService jobService,
        IFileStorage storage,
        ILogger<ModelsController> logger)
    {
        _modelService = modelService;
        _scenarioManager = scenarioManager;
        _jobService = jobService;
        _storage = storage;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetModels(string scenarioId)
    {
        try
        {
            var models = await _modelService.GetModelsAsync(scenarioId);
            return Ok(models);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{modelId}")]
    public async Task<IActionResult> GetModel(string scenarioId, string modelId)
    {
        try
        {
            var model = await _modelService.GetModelAsync(scenarioId, modelId);
            if (model == null)
                return NotFound();
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model {ModelId} for scenario {ScenarioId}", modelId, scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{modelId}/logs/train")]
    public async Task<IActionResult> GetModelTrainLogs(string scenarioId, string modelId)
    {
        try
        {
            var logs = await _modelService.GetModelTrainLogsAsync(scenarioId, modelId);
            if (logs == null)
                return NotFound();
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving train logs for model {ModelId} in scenario {ScenarioId}", modelId, scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{modelId}/metrics")]
    public async Task<IActionResult> GetModelMetrics(string scenarioId, string modelId)
    {
        try
        {
            var metrics = await _modelService.GetModelMetricsAsync(scenarioId, modelId);
            if (metrics == null)
                return NotFound();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics for model {ModelId} in scenario {ScenarioId}", modelId, scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("best-model")]
    public async Task<IActionResult> GetBestModel(string scenarioId)
    {
        try
        {
            // 최고 성능 모델 찾기
            var bestModel = await _modelService.GetBestModelAsync(scenarioId);
            if (bestModel == null)
                return NotFound(new { message = "No models found" });

            // 시나리오 메타데이터 업데이트
            var scenario = await _scenarioManager.LoadScenarioAsync(scenarioId);
            if (scenario != null && scenario.BestModelId != bestModel.ModelId)
            {
                scenario.BestModelId = bestModel.ModelId;
                await _scenarioManager.SaveScenarioAsync(scenarioId, scenario);
                _logger.LogInformation(
                    "Updated best model ID to {ModelId} for scenario {ScenarioId}",
                    bestModel.ModelId, scenarioId);
            }

            return Ok(bestModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving best model for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { message = "Internal server error while retrieving best model" });
        }
    }

    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupModels(string scenarioId)
    {
        try
        {
            var bestModel = await _modelService.GetBestModelAsync(scenarioId);
            if (bestModel == null)
            {
                return NotFound(new { message = "No models found" });
            }

            var modelsDir = _storage.GetScenarioModelsDir(scenarioId);
            var modelDirs = Directory.GetDirectories(modelsDir);

            foreach (var modelDir in modelDirs)
            {
                var modelId = Path.GetFileName(modelDir);
                if (modelId != bestModel.ModelId)
                {
                    Directory.Delete(modelDir, true);
                    _logger.LogInformation("Deleted model {ModelId}", modelId);
                }
            }

            return Ok(new { message = "Non-best models cleaned up successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up models for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { message = "Error cleaning up models" });
        }
    }
}