using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Models.Scenarios;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/models")]
public class ModelsController : ControllerBase
{
    private readonly ModelService _modelService;
    private readonly ScenarioService _scenarioService;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(
        ModelService modelService,
        ScenarioService scenarioService,
        ILogger<ModelsController> logger)
    {
        _modelService = modelService;
        _scenarioService = scenarioService;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error retrieving models" });
        }
    }

    [HttpGet("{modelId}")]
    public async Task<IActionResult> GetModel(string scenarioId, string modelId)
    {
        try
        {
            var model = await _modelService.GetAsync(scenarioId, modelId);
            if (model == null)
                return NotFound(new { error = $"Model {modelId} not found in scenario {scenarioId}" });
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model {ModelId} in scenario {ScenarioId}", modelId, scenarioId);
            return StatusCode(500, new { error = "Error retrieving model" });
        }
    }

    [HttpGet("{modelId}/logs/train")]
    public async Task<IActionResult> GetModelTrainLogs(string scenarioId, string modelId)
    {
        try
        {
            var logs = await _modelService.GetModelTrainLogsAsync(scenarioId, modelId);
            if (logs == null)
                return NotFound(new { error = "Training logs not found" });
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving train logs for model {ModelId} in scenario {ScenarioId}", modelId, scenarioId);
            return StatusCode(500, new { error = "Error retrieving training logs" });
        }
    }

    [HttpGet("{modelId}/metrics")]
    public async Task<IActionResult> GetModelMetrics(string scenarioId, string modelId)
    {
        try
        {
            var metrics = await _modelService.GetModelMetricsAsync(scenarioId, modelId);
            if (metrics == null)
                return NotFound(new { error = "Model metrics not found" });
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics for model {ModelId} in scenario {ScenarioId}", modelId, scenarioId);
            return StatusCode(500, new { error = "Error retrieving model metrics" });
        }
    }

    [HttpGet("best-model")]
    public async Task<IActionResult> GetBestModel(string scenarioId)
    {
        try
        {
            var bestModel = await _modelService.GetBestModelAsync(scenarioId);
            if (bestModel == null)
                return NotFound(new { error = "No models found in scenario" });

            // 시나리오 메타데이터의 BestModelId 업데이트
            var scenario = await _scenarioService.GetAsync(scenarioId);
            if (scenario != null && scenario.BestModelId != bestModel.ModelId)
            {
                await _scenarioService.UpdateAsync(scenarioId, new UpdateScenarioRequest
                {
                    BestModelId = bestModel.ModelId
                });

                _logger.LogInformation(
                    "Updated best model ID to {ModelId} for scenario {ScenarioId}",
                    bestModel.ModelId, scenarioId);
            }

            return Ok(bestModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving best model for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error retrieving best model" });
        }
    }

    [HttpDelete("{modelId}")]
    public async Task<IActionResult> DeleteModel(string scenarioId, string modelId)
    {
        try
        {
            var model = await _modelService.GetAsync(scenarioId, modelId);
            if (model == null)
                return NotFound(new { error = $"Model {modelId} not found in scenario {scenarioId}" });

            await _modelService.DeleteAsync(scenarioId, modelId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model {ModelId} in scenario {ScenarioId}", modelId, scenarioId);
            return StatusCode(500, new { error = "Error deleting model" });
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
                return NotFound(new { error = "No models found in scenario" });
            }

            var models = await _modelService.GetModelsAsync(scenarioId);
            var modelsToDelete = models.Where(m => m.ModelId != bestModel.ModelId);
            var deletedCount = 0;

            foreach (var model in modelsToDelete)
            {
                await _modelService.DeleteAsync(scenarioId, model.ModelId);
                deletedCount++;
            }

            return Ok(new
            {
                message = "Non-best models cleaned up successfully",
                bestModelId = bestModel.ModelId,
                deletedCount = deletedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up models for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error cleaning up models" });
        }
    }

    [HttpPost("{modelId}/validate")]
    public async Task<IActionResult> ValidateModel(string scenarioId, string modelId)
    {
        try
        {
            var model = await _modelService.GetAsync(scenarioId, modelId);
            if (model == null)
                return NotFound(new { error = $"Model {modelId} not found in scenario {scenarioId}" });

            // 모델 파일 존재 여부, 메타데이터 유효성 등 검증
            var validationResult = await _modelService.ValidateModelAsync(scenarioId, modelId);
            return Ok(validationResult);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating model {ModelId} in scenario {ScenarioId}", modelId, scenarioId);
            return StatusCode(500, new { error = "Error validating model" });
        }
    }
}