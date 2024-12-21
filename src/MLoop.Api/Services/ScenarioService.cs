using MLoop.Api.Models;
using MLoop.Models;
using MLoop.Models.Jobs;
using MLoop.Services;

namespace MLoop.Api.Services;

public class ScenarioService
{
    private readonly ScenarioManager _scenarioManager;
    private readonly JobService _jobService;
    private readonly ILogger<ScenarioService> _logger;

    public ScenarioService(
        ScenarioManager scenarioManager,
        JobService jobService,
        ILogger<ScenarioService> logger)
    {
        _scenarioManager = scenarioManager;
        _jobService = jobService;
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

    public Task DeleteScenarioAsync(string scenarioId)
    {
        return _scenarioManager.DeleteScenarioAsync(scenarioId);
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