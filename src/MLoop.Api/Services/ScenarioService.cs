using MLoop.Api.Models.Scenarios;
using MLoop.Models.Jobs;
using MLoop.Models.Workflows;
using MLoop.Models;
using MLoop.Services;
using MLoop.Storages;

namespace MLoop.Api.Services;

public class ScenarioService
{
    private readonly ScenarioManager _scenarioManager;
    private readonly ScenarioHandler _scenarioHandler;
    private readonly JobService _jobService;
    private readonly WorkflowService _workflowService;
    private readonly IFileStorage _storage;
    private readonly ILogger<ScenarioService> _logger;

    public ScenarioService(
        ScenarioManager scenarioManager,
        ScenarioHandler scenarioHandler,
        JobService jobService,
        WorkflowService workflowService,
        IFileStorage storage,
        ILogger<ScenarioService> logger)
    {
        _scenarioManager = scenarioManager;
        _scenarioHandler = scenarioHandler;
        _jobService = jobService;
        _workflowService = workflowService;
        _storage = storage;
        _logger = logger;
    }

    public Task<(bool isValid, List<string> issues)> ValidateTrainingPrerequisitesAsync(string scenarioId)
    {
        var issues = new List<string>();
        var dataDir = _storage.GetScenarioDataDir(scenarioId);

        if (!Directory.Exists(dataDir))
        {
            issues.Add("Training data directory not found");
            return Task.FromResult<(bool isValid, List<string> issues)>((false, issues));
        }

        if (!Directory.EnumerateFiles(dataDir, "*.*", SearchOption.AllDirectories).Any())
        {
            issues.Add("No training data files found");
            return Task.FromResult<(bool isValid, List<string> issues)>((false, issues));
        }

        return Task.FromResult<(bool isValid, List<string> issues)>((true, issues));
    }

    public async Task<object?> GetScenarioStatusAsync(string scenarioId)
    {
        var scenario = await GetAsync(scenarioId);
        if (scenario == null)
            return null;

        var jobs = await _jobService.GetScenarioJobsAsync(scenarioId);
        var latestJob = jobs.OrderByDescending(j => j.CreatedAt).FirstOrDefault();

        return new
        {
            scenario.ScenarioId,
            scenario.Name,
            scenario.MLType,
            HasTrainingData = Directory.Exists(_storage.GetScenarioDataDir(scenarioId)),
            LatestJobStatus = latestJob?.Status.ToString(),
            LatestJobType = latestJob?.Type.ToString(),
            LastUpdated = latestJob?.CompletedAt ?? scenario.CreatedAt
        };
    }

    public async Task<MLScenario> CreateAsync(CreateScenarioRequest request)
    {
        var scenario = await _scenarioHandler.InitializeScenarioAsync(request);
        _logger.LogInformation("Created new scenario with ID: {ScenarioId}", scenario.ScenarioId);
        return scenario;
    }

    public async Task<MLScenario?> GetAsync(string scenarioId)
    {
        return await _scenarioManager.LoadAsync(scenarioId);
    }

    public async Task<IEnumerable<MLScenario>> GetAllScenariosAsync()
    {
        return await _scenarioManager.GetAllScenariosAsync();
    }

    public async Task<MLScenario> UpdateAsync(string scenarioId, UpdateScenarioRequest request)
    {
        var scenario = await _scenarioHandler.UpdateScenarioAsync(scenarioId, request);
        _logger.LogInformation("Updated scenario {ScenarioId}", scenarioId);
        return scenario;
    }

    public async Task DeleteAsync(string scenarioId)
    {
        await _scenarioManager.DeleteAsync(scenarioId);
        _logger.LogInformation("Deleted scenario {ScenarioId}", scenarioId);
    }

    public async Task<(string jobId, MLJobStatus status)> CreateTrainJobAsync(
        string scenarioId,
        string workflowName = "default_train")
    {
        var scenario = await GetAsync(scenarioId)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found");

        var workflow = await _workflowService.GetAsync(scenarioId, workflowName);
        if (workflow == null || workflow.Type != JobTypes.Train)
        {
            throw new WorkflowNotFoundException(workflowName, scenarioId);
        }

        await ValidateTrainingPrerequisites(scenarioId);

        // 이미 실행 중인 training job이 있는지 확인
        var existingJobs = await _jobService.GetScenarioJobsAsync(scenarioId);
        var runningJob = existingJobs.FirstOrDefault(j =>
            j.WorkflowName == workflowName &&
            (j.Status == MLJobStatus.Waiting || j.Status == MLJobStatus.Running));

        if (runningJob != null)
        {
            return (runningJob.JobId, runningJob.Status);
        }

        // 새 training job 생성
        var jobId = await _jobService.CreateTrainJobAsync(
            scenarioId,
            workflowName,
            workflow.Environment);

        return (jobId, MLJobStatus.Waiting);
    }

    private Task ValidateTrainingPrerequisites(string scenarioId)
    {
        var dataDir = _storage.GetScenarioDataDir(scenarioId);
        if (!Directory.Exists(dataDir) || !Directory.EnumerateFiles(dataDir, "*.*", SearchOption.AllDirectories).Any())
        {
            throw new ValidationException("No training data found. Please upload data files first.");
        }

        return Task.CompletedTask;
    }
}