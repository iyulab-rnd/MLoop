using System.Text.Json;
using MLoop.Models;
using MLoop.Storages;

namespace MLoop.Worker.Services;

public class LocalJobQueue
{
    private readonly IFileStorage _storage;

    public LocalJobQueue(IFileStorage storage)
    {
        _storage = storage;
    }

    public JobStatus? DequeueJob()
    {
        var jobsDir = Path.Combine(Path.GetDirectoryName(_storage.GetScenarioBaseDir("dummy"))!, "jobs");
        if (!Directory.Exists(jobsDir)) return null;

        var jobFiles = Directory.GetFiles(jobsDir, "*.json")
            .Where(f => !f.EndsWith("_result.json")).ToArray();

        foreach (var jobFile in jobFiles)
        {
            var json = File.ReadAllText(jobFile);
            var job = JsonSerializer.Deserialize<JobStatus>(json);
            if (job != null && job.Status == "Waiting")
            {
                return job;
            }
        }

        return null;
    }
}
