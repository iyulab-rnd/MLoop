using MLoop.Api.Models.Jobs;
using MLoop.Base;
using MLoop.Models.Jobs;
using MLoop.Services;

namespace MLoop.Api.Services.Handlers;

public class JobHandler : HandlerBase<MLJob>
{
    private readonly JobManager _jobManager;

    public JobHandler(
        JobManager jobManager,
        ILogger<JobHandler> logger)
        : base(logger)
    {
        _jobManager = jobManager;
    }

    public override async Task<MLJob> ProcessAsync(MLJob job)
    {
        await _jobManager.SaveAsync(job.ScenarioId, job.JobId, job);
        return job;
    }

    public override Task ValidateAsync(MLJob job)
    {
        if (string.IsNullOrEmpty(job.ScenarioId))
            throw new ValidationException("ScenarioId is required");

        if (string.IsNullOrEmpty(job.JobId))
            throw new ValidationException("JobId is required");

        if (string.IsNullOrEmpty(job.WorkflowName))
            throw new ValidationException("WorkflowName is required");

        return Task.CompletedTask;
    }

    public async Task<MLJob> InitializeJobAsync(string scenarioId, CreateJobRequest request)
    {
        var job = new MLJob
        {
            JobId = request.JobId ?? Guid.NewGuid().ToString("N"),
            ScenarioId = scenarioId,
            WorkflowName = request.WorkflowName,
            JobType = request.JobType,
            Status = MLJobStatus.Waiting,
            CreatedAt = DateTime.UtcNow,
            ModelId = request.ModelId,
            Variables = request.Variables ?? []
        };

        await ValidateAsync(job);
        return await ProcessAsync(job);
    }

    public async Task ProcessCancellationAsync(string scenarioId, string jobId)
    {
        var job = await _jobManager.LoadAsync(scenarioId, jobId)
            ?? throw new KeyNotFoundException($"Job {jobId} not found in scenario {scenarioId}");

        if (job.Status is MLJobStatus.Completed or MLJobStatus.Failed)
            return; // Already in terminal state

        job.Status = MLJobStatus.Failed;
        job.FailureType = JobFailureType.None;
        job.ErrorMessage = "Job cancelled by user";
        job.WorkerId = null;
        job.AddStatusHistory(MLJobStatus.Failed, null, "Job cancelled by user");

        await _jobManager.SaveAsync(scenarioId, jobId, job);
    }
}
