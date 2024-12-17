using MLoop.Api.Models;
using MLoop.Models;
using MLoop.Services;

namespace MLoop.Api.Services;

public class ScenarioService
{
    private readonly ScenarioManager _scenarioManager;
    private readonly JobManager _jobManager;
    private readonly ILogger<ScenarioService> _logger;

    public ScenarioService(
        ScenarioManager scenarioManager,
        JobManager jobManager,
        ILogger<ScenarioService> logger)
    {
        _scenarioManager = scenarioManager;
        _jobManager = jobManager;
        _logger = logger;
    }

    public async Task<ScenarioMetadata> CreateScenarioAsync(CreateScenarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Scenario name cannot be empty", nameof(request));

        if (!IsValidCommand(request.Command))
            throw new ArgumentException("Invalid ML command type", nameof(request));

        var scenarioId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("Creating new scenario with ID: {ScenarioId}", scenarioId);

        var scenario = new ScenarioMetadata
        {
            ScenarioId = scenarioId,
            Name = request.Name.Trim(),
            Command = request.Command.Trim().ToLower(),
            Tags = request.Tags?.Select(t => t.Trim().ToLower()).ToList() ?? [],
            CreatedAt = DateTime.UtcNow,
            Models = []
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

        if (!IsValidCommand(request.Command))
            throw new ArgumentException("Invalid ML command type", nameof(request));

        var scenario = await _scenarioManager.LoadScenarioAsync(scenarioId)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found");

        scenario.Name = request.Name.Trim();
        scenario.Command = request.Command.Trim().ToLower();
        scenario.Tags = request.Tags?.Select(t => t.Trim().ToLower()).ToList() ?? [];

        await _scenarioManager.SaveScenarioAsync(scenarioId, scenario);
    }

    public Task DeleteScenarioAsync(string scenarioId)
    {
        return _scenarioManager.DeleteScenarioAsync(scenarioId);
    }

    public async Task<string> CreateTrainJobAsync(string scenarioId, string command)
    {
        var scenario = await _scenarioManager.LoadScenarioAsync(scenarioId)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found");

        // 명령어가 시나리오의 ML 타입과 일치하는지 검증
        if (!command.StartsWith(scenario.Command))
        {
            throw new ArgumentException($"Command must be compatible with scenario type: {scenario.Command}");
        }

        var modelId = Guid.NewGuid().ToString("N");

        var job = new JobStatus
        {
            JobId = Guid.NewGuid().ToString("N"),
            ScenarioId = scenarioId,
            JobType = "Train",
            Status = "Waiting",
            CreatedAt = DateTime.UtcNow,
            Command = command,
            ModelId = modelId
        };

        _jobManager.SaveJobStatus(job);
        _logger.LogInformation("Created training job {JobId} for scenario {ScenarioId}", job.JobId, scenarioId);
        return job.JobId;
    }

    public async Task<string> CreatePredictJobAsync(string scenarioId)
    {
        var scenario = await _scenarioManager.LoadScenarioAsync(scenarioId)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found");

        if (scenario.Models.Count == 0)
            throw new InvalidOperationException("No trained models available");

        // 가장 최근 모델 사용 (실제로는 성능 기반으로 선택하는 로직이 필요할 수 있음)
        var modelId = scenario.Models.Last();

        var job = new JobStatus
        {
            JobId = Guid.NewGuid().ToString("N"),
            ScenarioId = scenarioId,
            JobType = "Predict",
            Status = "Waiting",
            CreatedAt = DateTime.UtcNow,
            ModelId = modelId
        };

        _jobManager.SaveJobStatus(job);
        _logger.LogInformation("Created prediction job {JobId} for scenario {ScenarioId} using model {ModelId}",
            job.JobId, scenarioId, modelId);
        return job.JobId;
    }

    public async Task AddModelAsync(string scenarioId, string modelId)
    {
        var scenario = await _scenarioManager.LoadScenarioAsync(scenarioId)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found");

        if (!scenario.Models.Contains(modelId))
        {
            scenario.Models.Add(modelId);
            await _scenarioManager.SaveScenarioAsync(scenarioId, scenario);
            _logger.LogInformation("Added model {ModelId} to scenario {ScenarioId}", modelId, scenarioId);
        }
    }

    private static bool IsValidCommand(string command)
    {
        var validCommands = new[]
        {
            "classification",
            "regression",
            "recommendation",
            "image-classification",
            "text-classification",
            "forecasting",
            "object-detection"
        };

        return validCommands.Contains(command.Trim().ToLower());
    }
}