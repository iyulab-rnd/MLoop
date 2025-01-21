using MLoop.Api.Models.Jobs;
using MLoop.Base;
using MLoop.Models.Jobs;
using MLoop.Services;

namespace MLoop.Api.Services;

public class JobService : ScenarioServiceBase<MLJob, CreateJobRequest, UpdateJobRequest>
{
    private readonly JobManager _jobManager;
    private readonly JobHandler _jobHandler;

    public JobService(
        JobManager jobManager,
        JobHandler jobHandler,
        ILogger<JobService> logger)
        : base(logger)
    {
        _jobManager = jobManager;
        _jobHandler = jobHandler;
    }

    public override async Task<MLJob> CreateAsync(string scenarioId, CreateJobRequest request)
    {
        return await _jobHandler.InitializeJobAsync(scenarioId, request);
    }

    public override async Task<MLJob?> GetAsync(string scenarioId, string jobId)
    {
        return await _jobManager.LoadAsync(scenarioId, jobId);
    }

    public override async Task<MLJob> UpdateAsync(string scenarioId, string jobId, UpdateJobRequest request)
    {
        var job = await _jobManager.LoadAsync(scenarioId, jobId)
            ?? throw new KeyNotFoundException($"Job {jobId} not found in scenario {scenarioId}");

        if (request.Status.HasValue)
        {
            await _jobManager.UpdateJobStatusAsync(job, request.Status.Value, request.WorkerId, request.Message);
        }

        if (request.Variables != null && request.Variables.Any())
        {
            foreach (var (key, value) in request.Variables)
            {
                job.Variables[key] = value;
            }
            await _jobManager.SaveAsync(scenarioId, jobId, job);
        }

        return job;
    }

    public override async Task DeleteAsync(string scenarioId, string jobId)
    {
        await _jobManager.DeleteAsync(scenarioId, jobId);
    }

    public async Task<IEnumerable<MLJob>> GetScenarioJobsAsync(string scenarioId)
    {
        return await _jobManager.GetScenarioJobsAsync(scenarioId);
    }

    public async Task<string> CreateJobAsync(
        string scenarioId,
        string workflowName,
        Dictionary<string, object>? variables = null)
    {
        var request = new CreateJobRequest
        {
            WorkflowName = workflowName,
            JobType = MLJobType.Train,
            Variables = variables
        };

        var job = await CreateAsync(scenarioId, request);
        return job.JobId;
    }

    public async Task<string> CreatePredictionJobAsync(
        string scenarioId,
        string modelId,
        string predictionId,
        Dictionary<string, object>? variables = null)
    {
        var request = new CreateJobRequest
        {
            JobId = predictionId,
            JobType = MLJobType.Predict,
            WorkflowName = "default_predict",
            ModelId = modelId,
            Variables = variables
        };

        var job = await CreateAsync(scenarioId, request);
        return job.JobId;
    }

    public async Task CancelJobAsync(string scenarioId, string jobId)
    {
        await _jobHandler.ProcessCancellationAsync(scenarioId, jobId);
    }

    public async Task<string?> GetJobLogsAsync(string scenarioId, string jobId)
    {
        var logsPath = _jobManager.GetJobLogsPath(scenarioId, jobId);
        if (!File.Exists(logsPath))
        {
            _logger.LogWarning("Logs not found for job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(logsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading logs for job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
            throw;
        }
    }

    public async Task<MLJobResult?> GetJobResultAsync(string scenarioId, string jobId)
    {
        return await _jobManager.GetJobResultAsync(scenarioId, jobId);
    }

    public async Task SaveJobResultAsync(string scenarioId, string jobId, MLJobResult result)
    {
        await _jobManager.SaveJobResultAsync(scenarioId, jobId, result);
    }

    internal Task<MLJob?> GetPredictionJobAsync(string scenarioId, string predictionId)
    {
        // predictionId is the same as jobId
        return GetAsync(scenarioId, predictionId);
    }
}
