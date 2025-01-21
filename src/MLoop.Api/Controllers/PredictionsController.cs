using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Services;
using MLoop.Models.Jobs;
using MLoop.Models.Workflows;
using MLoop.Storages;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/predictions")]
public class PredictionsController : ControllerBase
{
    private readonly IFileStorage _storage;
    private readonly JobService _jobService;
    private readonly ModelService _modelService;
    private readonly WorkflowService _workflowService;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(
        IFileStorage storage,
        JobService jobService,
        ModelService modelService,
        WorkflowService workflowService,
        ILogger<PredictionsController> logger)
    {
        _storage = storage;
        _jobService = jobService;
        _modelService = modelService;
        _workflowService = workflowService;
        _logger = logger;
    }

    [HttpPost]
    [Route("/api/scenarios/{scenarioId}/predict")]
    [Route("/api/scenarios/{scenarioId}/predictions")]
    [Route("/api/scenarios/{scenarioId}/models/{modelId}/predict")]
    public async Task<IActionResult> Predict(
    string scenarioId,
    [FromBody] string content,
    [FromQuery] string? modelId = null,
    [FromQuery] string workflowName = "default_predict")
    {
        try
        {
            // 워크플로우 검증
            var workflow = await _workflowService.GetAsync(scenarioId, workflowName);
            if (workflow == null || workflow.Type != WorkflowType.Predict)
            {
                return BadRequest(new { message = $"Invalid prediction workflow '{workflowName}'" });
            }

            // 모델 선택
            var selectedModel = modelId != null
                ? await _modelService.GetAsync(scenarioId, modelId)
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
            var environment = new Dictionary<string, object>(workflow.Environment)
            {
                ["modelId"] = selectedModel.ModelId,
                ["predictionId"] = predictionId
            };

            await _jobService.CreateJobAsync(scenarioId, workflowName, environment);

            return Ok(new
            {
                predictionId,
                modelId = selectedModel.ModelId,
                workflowName,
                isUsingBestModel = modelId == null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prediction for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Error creating prediction");
        }
    }

    [HttpPost("image-classification")]
    public async Task<IActionResult> CreateImageClassificationPrediction(
        string scenarioId,
        [FromForm] IFormFile file,
        [FromQuery] string? modelId = null)
    {
        try
        {
            // 1. 모델 검증 및 선택
            var selectedModel = modelId != null
                ? await _modelService.GetAsync(scenarioId, modelId)
                : await _modelService.GetBestModelAsync(scenarioId);

            if (selectedModel == null)
            {
                var message = modelId != null
                    ? $"Model {modelId} not found"
                    : "No trained models found for this scenario";
                return NotFound(new { message });
            }

            // MLType이 image-classification인지 확인
            if (selectedModel.MLType != "image-classification")
            {
                return BadRequest(new { message = "Selected model is not an image classification model" });
            }

            // 2. 예측 ID 생성 및 디렉토리 준비
            var predictionId = Guid.NewGuid().ToString("N");
            var predictionDir = _storage.GetPredictionDir(scenarioId, predictionId);
            Directory.CreateDirectory(predictionDir);

            // 3. 파일 저장
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(predictionDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 4. 예측 작업 생성
            await _jobService.CreatePredictionJobAsync(
                scenarioId,
                selectedModel.ModelId,
                predictionId,
                new Dictionary<string, object>
                {
                    ["fileName"] = fileName
                });

            return Ok(new
            {
                predictionId,
                modelId = selectedModel.ModelId,
                fileName,
                isUsingBestModel = modelId == null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating image classification prediction for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { message = "Error creating prediction" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPredictions(string scenarioId)
    {
        try
        {
            var predictionsDir = _storage.GetPredictionsDir(scenarioId);
            if (!Directory.Exists(predictionsDir))
            {
                return Ok(new List<object>());
            }

            var predictions = new List<object>();
            var predictionDirs = Directory.GetDirectories(predictionsDir);

            foreach (var predictionDir in predictionDirs)
            {
                var predictionId = Path.GetFileName(predictionDir);
                var job = await _jobService.GetPredictionJobAsync(scenarioId, predictionId);

                if (job != null)
                {
                    var resultPath = _storage.GetPredictionResultPath(scenarioId, predictionId);
                    var inputPath = Directory.GetFiles(predictionDir, "input.*").FirstOrDefault();

                    predictions.Add(new
                    {
                        predictionId = job.JobId,
                        modelId = job.ModelId,
                        status = job.Status.ToString(),
                        createdAt = job.CreatedAt,
                        completedAt = job.CompletedAt,
                        errorMessage = job.ErrorMessage,
                        hasResult = System.IO.File.Exists(resultPath),
                        inputFile = inputPath != null ? Path.GetFileName(inputPath) : null
                    });
                }
            }

            return Ok(predictions.OrderByDescending(p => ((DateTime)p.GetType().GetProperty("createdAt")!.GetValue(p)!)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving predictions for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { message = "Error retrieving predictions" });
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

    [HttpGet("{predictionId}/files")]
    public IActionResult GetPredictionFiles(string scenarioId, string predictionId)
    {
        try
        {
            var predictionDir = _storage.GetPredictionDir(scenarioId, predictionId);
            if (!Directory.Exists(predictionDir))
            {
                return NotFound(new { message = "Prediction not found" });
            }

            var files = Directory.GetFiles(predictionDir)
                .Select(f => new
                {
                    name = Path.GetFileName(f),
                    path = Path.GetRelativePath(predictionDir, f),
                    size = new FileInfo(f).Length,
                    lastModified = System.IO.File.GetLastWriteTimeUtc(f)
                });

            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error listing files for prediction {PredictionId} in scenario {ScenarioId}",
                predictionId, scenarioId);
            return StatusCode(500, new { message = "Error listing prediction files" });
        }
    }

    [HttpGet("{predictionId}/files/{*filePath}")]
    public IActionResult DownloadFile(string scenarioId, string predictionId, string filePath)
    {
        try
        {
            var predictionDir = _storage.GetPredictionDir(scenarioId, predictionId);
            if (!Directory.Exists(predictionDir))
            {
                return NotFound(new { message = "Prediction not found" });
            }

            // 경로 조작 방지
            if (filePath.Contains("..") || Path.IsPathRooted(filePath))
            {
                return BadRequest(new { message = "Invalid file path" });
            }

            var fullPath = Path.Combine(predictionDir, filePath);

            // 디렉토리 트래버설 방지를 위한 추가 검증
            if (!Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(predictionDir)))
            {
                return BadRequest(new { message = "Invalid file path" });
            }

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new { message = "File not found" });
            }

            // Content-Type 결정
            var contentType = filePath.ToLower() switch
            {
                string s when s.EndsWith(".csv") => "text/csv",
                string s when s.EndsWith(".tsv") => "text/tab-separated-values",
                string s when s.EndsWith(".txt") => "text/plain",
                string s when s.EndsWith(".json") => "application/json",
                string s when s.EndsWith(".yaml") || s.EndsWith(".yml") => "application/x-yaml",
                _ => "application/octet-stream"
            };

            // 파일 스트림 반환
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return File(fileStream, contentType, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error downloading file {FilePath} for prediction {PredictionId} in scenario {ScenarioId}",
                filePath, predictionId, scenarioId);
            return StatusCode(500, new { message = "Error downloading file" });
        }
    }
}