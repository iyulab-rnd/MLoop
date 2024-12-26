using MLoop.Api.Models;
using MLoop.Models;
using MLoop.Models.Jobs;
using MLoop.Services;
using MLoop.Storages;

namespace MLoop.Api.Services;

public class ScenarioService
{
    private readonly ScenarioManager _scenarioManager;
    private readonly JobService _jobService;
    private readonly IFileStorage _storage;
    private readonly ILogger<ScenarioService> _logger;

    public ScenarioService(
        ScenarioManager scenarioManager,
        JobService jobService,
        IFileStorage storage,
        ILogger<ScenarioService> logger)
    {
        _scenarioManager = scenarioManager;
        _jobService = jobService;
        _storage = storage;
        _logger = logger;
    }

    public async Task<ScenarioMetadata> CreateScenarioAsync(CreateScenarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Scenario name cannot be empty", nameof(request));

        if (!IsValidMLType(request.MLType))
            throw new ArgumentException("Invalid ML Type", nameof(request));

        var scenarioId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("Creating new scenario with ID: {ScenarioId}", scenarioId);

        var scenario = new ScenarioMetadata
        {
            ScenarioId = scenarioId,
            Name = request.Name.Trim(),
            MLType = request.MLType.Trim().ToLower(),
            Tags = request.Tags?.Select(t => t.Trim().ToLower()).ToList() ?? [],
            CreatedAt = DateTime.UtcNow
        };

        await _scenarioManager.SaveScenarioAsync(scenarioId, scenario);
        _logger.LogInformation("Successfully created scenario: {ScenarioId}", scenarioId);
        return scenario;
    }

    public async Task<IQueryable<ScenarioMetadata>> GetScenariosAsync()
    {
        _logger.LogInformation("Getting all scenarios");
        var scenarios = await _scenarioManager.GetAllScenariosAsync();
        _logger.LogInformation("Found {Count} scenarios", scenarios.Count);
        return scenarios.AsQueryable();
    }

    public async Task<ScenarioMetadata?> GetScenarioAsync(string scenarioId)
    {
        return await _scenarioManager.LoadScenarioAsync(scenarioId);
    }

    public async Task UpdateScenarioAsync(string scenarioId, CreateScenarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Scenario name cannot be empty", nameof(request));

        if (!IsValidMLType(request.MLType))
            throw new ArgumentException("Invalid ML Type", nameof(request));

        var scenario = await _scenarioManager.LoadScenarioAsync(scenarioId)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found");

        scenario.Name = request.Name.Trim();
        scenario.MLType = request.MLType.Trim().ToLower();
        scenario.Tags = request.Tags?.Select(t => t.Trim().ToLower()).ToList() ?? [];

        await _scenarioManager.SaveScenarioAsync(scenarioId, scenario);
    }

    public async Task DeleteScenarioAsync(string scenarioId)
    {
        try
        {
            // 1. 시나리오 메타데이터 확인
            var scenario = await _scenarioManager.LoadScenarioAsync(scenarioId);
            if (scenario == null)
            {
                _logger.LogWarning("Attempting to delete non-existent scenario {ScenarioId}", scenarioId);
                throw new KeyNotFoundException($"Scenario {scenarioId} not found");
            }

            // 2. 시나리오 기본 디렉토리 경로 가져오기
            var scenarioBaseDir = _storage.GetScenarioBaseDir(scenarioId);

            // 3. 시나리오 메타데이터 삭제
            await _scenarioManager.DeleteScenarioAsync(scenarioId);

            // 4. 시나리오 디렉토리가 존재하면 삭제
            if (Directory.Exists(scenarioBaseDir))
            {
                // 읽기 전용 파일 속성 제거를 위한 재귀적 처리
                foreach (var file in Directory.GetFiles(scenarioBaseDir, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(scenarioBaseDir, recursive: true);
                _logger.LogInformation("Deleted scenario directory for {ScenarioId}", scenarioId);
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error deleting scenario {ScenarioId}", scenarioId);
            throw new ApiException($"Failed to delete scenario: {ex.Message}", 500);
        }
    }

    public async Task<(string jobId, MLJobStatus status)> CreateTrainJobAsync(string scenarioId)
    {
        var scenario = await _scenarioManager.LoadScenarioAsync(scenarioId)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found");

        // 완료되지 않은 Train 작업이 있는지 확인
        var jobs = await _jobService.GetScenarioJobsAsync(scenarioId);
        var existingTrainJob = jobs.FirstOrDefault(j =>
            j.JobType == MLJobType.Train &&
            (j.Status == MLJobStatus.Waiting || j.Status == MLJobStatus.Running));

        if (existingTrainJob != null)
        {
            _logger.LogInformation(
                "Found existing train job {JobId} in progress for scenario {ScenarioId}",
                existingTrainJob.JobId, scenarioId);
            return (existingTrainJob.JobId, existingTrainJob.Status);
        }

        // 새 작업 생성
        var jobId = await _jobService.CreateJobAsync(scenarioId, MLJobType.Train);
        _logger.LogInformation(
            "Created new train job {JobId} for scenario {ScenarioId}",
            jobId, scenarioId);

        return (jobId, MLJobStatus.Waiting);
    }

    private static bool IsValidMLType(string mlType)
    {
        var validTypes = new[]
        {
            "classification",
            "regression",
            "recommendation",
            "image-classification",
            "text-classification",
            "forecasting",
            "object-detection"
        };

        return validTypes.Contains(mlType.Trim().ToLower());
    }
}