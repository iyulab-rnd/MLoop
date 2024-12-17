using System.Text.Json;
using MLoop.Models;
using MLoop.Storages;

namespace MLoop.Services;

public class JobManager
{
    private readonly IFileStorage _storage;

    public JobManager(IFileStorage storage)
    {
        _storage = storage;
    }

    public string CreateJob(string scenarioId, string jobType)
    {
        string jobId = Guid.NewGuid().ToString("N");
        var job = new JobStatus
        {
            JobId = jobId,
            ScenarioId = scenarioId,
            JobType = jobType,
            Status = "Waiting",
            CreatedAt = DateTime.UtcNow
        };

        SaveJobStatus(job);
        return jobId;
    }

    public JobStatus? GetJobStatus(string jobId)
    {
        var path = _storage.GetJobPath(jobId);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<JobStatus>(json);
    }

    public void SaveJobStatus(JobStatus job)
    {
        var path = _storage.GetJobPath(job.JobId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
