using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Services;
using MLoop.Models.Jobs;
using MLoop.Storages;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/predictions")]
public class PredictionsController : ControllerBase
{
    private readonly IFileStorage _storage;
    private readonly JobService _jobService;
    private readonly ModelService _modelService;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(
        IFileStorage storage,
        JobService jobService,
        ModelService modelService,
        ILogger<PredictionsController> logger)
    {
        _storage = storage;
        _jobService = jobService;
        _modelService = modelService;
        _logger = logger;
    }

    [HttpPost]
    [Route("/scenarios/{scenarioId}/predict")]
    [Route("/scenarios/{scenarioId}/models/{modelId}/predict")]
    public async Task<IActionResult> Predict(
        string scenarioId,
        [FromBody] string content,
        [FromQuery] string? modelId = null)
    {
        try
        {
            // 모델 선택: modelId가 제공된 경우 해당 모델 사용, 아니면 Best Model 사용
            var selectedModel = modelId != null
                ? await _modelService.GetModelAsync(scenarioId, modelId)
                : await _modelService.GetBestModelAsync(scenarioId);

            if (selectedModel == null)
            {
                var message = modelId != null
                    ? $"Model {modelId} not found"
                    : "No trained models found for this scenario";
                return NotFound(new { message });
            }

            // Content-Type 검증
            var contentType = Request.ContentType?.ToLower() ?? "";
            var extension = contentType switch
            {
                "text/tab-separated-values" => ".tsv",
                "text/csv" => ".csv",
                _ => null
            };

            if (extension == null)
            {
                return BadRequest(new
                {
                    error = "Unsupported Content-Type. Supported types are: text/tab-separated-values, text/csv",
                    contentType
                });
            }

            // 예측 ID 생성 및 디렉토리 생성
            var predictionId = Guid.NewGuid().ToString("N");
            var predictionDir = _storage.GetPredictionDir(scenarioId, predictionId);
            Directory.CreateDirectory(predictionDir);

            // 입력 파일 저장
            var inputPath = _storage.GetPredictionInputPath(scenarioId, predictionId, extension);
            await System.IO.File.WriteAllTextAsync(inputPath, content);

            // 예측 작업 생성
            await _jobService.CreatePredictionJobAsync(scenarioId, selectedModel.ModelId, predictionId);

            return Ok(new
            {
                predictionId,
                modelId = selectedModel.ModelId,
                isUsingBestModel = modelId == null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prediction for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Error creating prediction");
        }
    }

    [HttpGet("{predictionId}")]
    public async Task<IActionResult> GetPredictionResult(string scenarioId, string predictionId)
    {
        try
        {
            var predictionDir = _storage.GetPredictionDir(scenarioId, predictionId);
            if (!Directory.Exists(predictionDir))
            {
                return NotFound(new { message = "Prediction not found" });
            }

            var resultPath = _storage.GetPredictionResultPath(scenarioId, predictionId);
            if (!System.IO.File.Exists(resultPath))
            {
                // 작업 상태 확인
                var job1 = await _jobService.GetPredictionJobAsync(scenarioId, predictionId);
                if (job1 == null)
                {
                    return StatusCode(500, new { message = "Prediction job not found" });
                }
                return Ok(new
                {
                    status = "processing",
                    jobStatus = job1.Status.ToString(),
                    message = job1.Status == MLJobStatus.Failed ? job1.ErrorMessage : "Processing prediction...",
                    modelId = job1.ModelId
                });
            }

            // 결과 파일이 있는 경우 CSV로 반환
            var resultContent = await System.IO.File.ReadAllTextAsync(resultPath);
            return File(
                System.Text.Encoding.UTF8.GetBytes(resultContent),
                "text/csv",
                $"result_{predictionId}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prediction result for {PredictionId}", predictionId);
            return StatusCode(500, new { message = "Error retrieving prediction result" });
        }
    }

    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupPredictions(string scenarioId)
    {
        try
        {
            var predictionsDir = _storage.GetPredictionsDir(scenarioId);
            if (!Directory.Exists(predictionsDir))
            {
                return Ok(new { message = "No predictions found to clean up" });
            }

            int cleanedCount = 0;
            var predictionDirs = Directory.GetDirectories(predictionsDir);

            foreach (var predictionDir in predictionDirs)
            {
                var predictionId = Path.GetFileName(predictionDir);

                // 작업 상태 확인
                var job = await _jobService.GetPredictionJobAsync(scenarioId, predictionId);

                // 완료되었거나 실패한 예측만 정리
                if (job?.Status is MLJobStatus.Completed or MLJobStatus.Failed)
                {
                    try
                    {
                        Directory.Delete(predictionDir, true);
                        cleanedCount++;
                        _logger.LogInformation(
                            "Cleaned up prediction {PredictionId} for scenario {ScenarioId}",
                            predictionId, scenarioId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to delete prediction directory {PredictionDir}",
                            predictionDir);
                    }
                }
            }

            return Ok(new
            {
                message = "Completed predictions cleaned up successfully",
                cleanedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error cleaning up predictions for scenario {ScenarioId}",
                scenarioId);
            return StatusCode(500, new { message = "Error cleaning up predictions" });
        }
    }
}