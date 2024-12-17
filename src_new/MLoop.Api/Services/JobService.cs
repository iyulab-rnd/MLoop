using MLoop.Services;

namespace MLoop.Api.Services;

public class JobService
{
    private readonly JobManager _jobManager;

    public JobService(JobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public string CreateJob(string scenarioId, string jobType)
    {
        return _jobManager.CreateJob(scenarioId, jobType);
    }

    public MLoop.Models.JobStatus? GetJobStatus(string jobId)
    {
        return _jobManager.GetJobStatus(jobId);
    }
}
