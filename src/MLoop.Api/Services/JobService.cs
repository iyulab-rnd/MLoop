using MLoop.Models.Jobs;
using MLoop.Services;
using MLoop.Storages;

namespace MLoop.Api.Services;

public partial class JobService
{
    private readonly JobManager _jobManager;
    private readonly IFileStorage _storage;
    private readonly ILogger<JobService> _logger;

    public JobService(JobManager jobManager, IFileStorage storage, ILogger<JobService> logger)
    {
        _jobManager = jobManager;
        _storage = storage;
        _logger = logger;
    }

    public async Task<string> CreateJobAsync(string scenarioId, MLJobType jobType)
    {
        try
        {
            var job = new MLJob
            {
                JobId = Guid.NewGuid().ToString("N"),
                ScenarioId = scenarioId,
                Status = MLJobStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                JobType = jobType
            };

            await _jobManager.SaveJobStatusAsync(job);
            _logger.LogInformation("Created job {JobId} of type {JobType} for scenario {ScenarioId}",
                job.JobId, jobType, scenarioId);

            return job.JobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job for scenario {ScenarioId}", scenarioId);
            throw;
        }
    }

    public async Task<MLJob?> GetJobStatusAsync(string scenarioId, string jobId)
    {
        try
        {
            var r = _jobManager.GetJobStatus(scenarioId, jobId);
            return await Task.FromResult(r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for scenario {ScenarioId}, job {JobId}",
                scenarioId, jobId);
            throw;
        }
    }

    public async Task<IEnumerable<MLJob>> GetScenarioJobsAsync(string scenarioId)
    {
        try
        {
            return await _jobManager.GetScenarioJobsAsync(scenarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get jobs for scenario {ScenarioId}", scenarioId);
            throw;
        }
    }

    public async Task UpdateJobStatusAsync(string scenarioId, string jobId, MLJobStatus status, string? errorMessage = null)
    {
        try
        {
            var job = _jobManager.GetJobStatus(scenarioId, jobId);
            if (job == null)
            {
                _logger.LogWarning("Cannot update status: Job {JobId} not found in scenario {ScenarioId}",
                    jobId, scenarioId);
                return;
            }

            job.Status = status;
            job.ErrorMessage = errorMessage;

            if (status is MLJobStatus.Completed or MLJobStatus.Failed)
            {
                job.CompletedAt = DateTime.UtcNow;
            }

            await _jobManager.SaveJobStatusAsync(job);
            _logger.LogInformation("Updated job {JobId} status to {Status} in scenario {ScenarioId}",
                jobId, status, scenarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job status for scenario {ScenarioId}, job {JobId}",
                scenarioId, jobId);
            throw;
        }
    }

    public async Task SaveJobResultAsync(string scenarioId, string jobId, MLJobResult result)
    {
        try
        {
            await _jobManager.SaveJobResultAsync(scenarioId, jobId, result);
            _logger.LogInformation("Saved result for job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save job result for scenario {ScenarioId}, job {JobId}",
                scenarioId, jobId);
            throw;
        }
    }

    public async Task<MLJobResult?> GetJobResultAsync(string scenarioId, string jobId)
    {
        try
        {
            return await _jobManager.GetJobResultAsync(scenarioId, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job result for scenario {ScenarioId}, job {JobId}",
                scenarioId, jobId);
            throw;
        }
    }

    public async Task<bool> IsJobCompletedAsync(string scenarioId, string jobId)
    {
        try
        {
            var job = await GetJobStatusAsync(scenarioId, jobId);
            return job?.Status is MLJobStatus.Completed or MLJobStatus.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check job completion status for scenario {ScenarioId}, job {JobId}",
                scenarioId, jobId);
            throw;
        }
    }

    public async Task CancelJobAsync(string scenarioId, string jobId)
    {
        var job = await GetJobStatusAsync(scenarioId, jobId)
            ?? throw new KeyNotFoundException($"Job {jobId} not found in scenario {scenarioId}");

        if (job.Status is MLJobStatus.Completed or MLJobStatus.Failed)
            return; // Already in terminal state

        await _jobManager.UpdateJobStatusAsync(
            job,
            MLJobStatus.Failed,
            failureType: JobFailureType.None,
            message: "Job cancelled by user");

        _logger.LogInformation("Cancelled job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
    }

    public async Task<string?> GetJobLogsAsync(string scenarioId, string jobId)
    {
        var logsPath = _storage.GetJobLogsPath(scenarioId, jobId);
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

    public async Task<string> CreatePredictionJobAsync(
        string scenarioId,
        string modelId,
        string predictionId)
    {
        try
        {
            var job = new MLJob
            {
                JobId = predictionId, // predictionId를 jobId로 사용
                ScenarioId = scenarioId,
                Status = MLJobStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                JobType = MLJobType.Predict,
                ModelId = modelId // 사용할 모델 ID 설정
            };

            await _jobManager.SaveJobStatusAsync(job);

            _logger.LogInformation(
                "Created prediction job {JobId} for scenario {ScenarioId} using model {ModelId}",
                job.JobId, scenarioId, modelId);

            return job.JobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create prediction job for scenario {ScenarioId}, model {ModelId}",
                scenarioId, modelId);
            throw;
        }
    }

    public async Task<MLJob?> GetPredictionJobAsync(string scenarioId, string predictionId)
    {
        try
        {
            return await Task.FromResult(_jobManager.GetJobStatus(scenarioId, predictionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get prediction job status for scenario {ScenarioId}, prediction {PredictionId}",
                scenarioId, predictionId);
            throw;
        }
    }

    public async Task<string> CreatePredictionJobAsync(
        string scenarioId,
        string modelId,
        string predictionId,
        Dictionary<string, object>? variables = null)
    {
        try
        {
            var job = new MLJob
            {
                JobId = predictionId,
                ScenarioId = scenarioId,
                Status = MLJobStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                JobType = MLJobType.Predict,
                ModelId = modelId
            };

            if (variables != null)
            {
                job.Variables = variables;
            }

            await _jobManager.SaveJobStatusAsync(job);

            _logger.LogInformation(
                "Created prediction job {JobId} for scenario {ScenarioId} using model {ModelId}",
                job.JobId, scenarioId, modelId);

            return job.JobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create prediction job for scenario {ScenarioId}, model {ModelId}",
                scenarioId, modelId);
            throw;
        }
    }
}