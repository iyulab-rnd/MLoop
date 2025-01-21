using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MLoop.Models.Jobs;
using MLoop.Models.Messages;
using System.Text;

namespace MLoop.Services;

public class JobManager : ScenarioManagerBase<MLJob>
{
    private readonly QueueClient? _scalingQueue;
    private const string JobResultFileName = "result.json";

    public JobManager(
        IFileStorage storage,
        ILogger<JobManager> logger,
        IConfiguration configuration)
        : base(storage, logger)
    {
        // Scaling Queue 초기화 (선택적)
        var queueConnection = configuration.GetConnectionString("QueueConnection");
        var queueName = configuration["Queue:ScalingQueueName"] ?? "mloop-scaling-queue";

        if (!string.IsNullOrEmpty(queueConnection))
        {
            try
            {
                var queueServiceClient = new QueueServiceClient(queueConnection);
                _scalingQueue = queueServiceClient.GetQueueClient(queueName);
                _scalingQueue.Create();
                _logger.LogInformation("Successfully initialized scaling queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize scaling queue. Will operate without queue support");
                _scalingQueue = null;
            }
        }
    }

    public override async Task<MLJob?> LoadAsync(string scenarioId, string jobId)
    {
        var path = _storage.GetJobPath(scenarioId, jobId);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Job file not found for scenarioId: {ScenarioId}, jobId: {JobId}",
                scenarioId, jobId);
            return null;
        }

        var json = await File.ReadAllTextAsync(path);
        var job = JsonHelper.Deserialize<MLJob>(json);
        if (job != null)
        {
            job.ScenarioId = scenarioId;
            job.JobId = jobId;
        }
        return job;
    }

    public override async Task SaveAsync(string scenarioId, string jobId, MLJob job)
    {
        var path = _storage.GetJobPath(scenarioId, jobId);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        var json = JsonHelper.Serialize(job);
        await File.WriteAllTextAsync(path, json);

        // Waiting 상태의 새 작업인 경우에만 scaling 통지
        if (job.Status == MLJobStatus.Waiting)
        {
            await SendScalingNotificationAsync(job);
        }

        _logger.LogInformation(
            "Saved job status for scenarioId: {ScenarioId}, jobId: {JobId}",
            job.ScenarioId, job.JobId);
    }

    public override Task DeleteAsync(string scenarioId, string jobId)
    {
        var jobPath = _storage.GetJobPath(scenarioId, jobId);
        var resultPath = _storage.GetJobResultPath(scenarioId, jobId);
        var logsPath = _storage.GetJobLogsPath(scenarioId, jobId);

        if (File.Exists(jobPath)) File.Delete(jobPath);
        if (File.Exists(resultPath)) File.Delete(resultPath);
        if (File.Exists(logsPath)) File.Delete(logsPath);

        return Task.CompletedTask;
    }

    public override Task<bool> ExistsAsync(string scenarioId, string jobId)
    {
        var path = _storage.GetJobPath(scenarioId, jobId);
        return Task.FromResult(File.Exists(path));
    }

    private async Task SendScalingNotificationAsync(MLJob job)
    {
        if (_scalingQueue == null) return;

        try
        {
            var scalingMessage = new ScalingMessage
            {
                JobId = job.JobId,
                ScenarioId = job.ScenarioId,
                Timestamp = DateTime.UtcNow
            };

            var message = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(JsonHelper.Serialize(scalingMessage))
            );

            await _scalingQueue.SendMessageAsync(message,
                visibilityTimeout: TimeSpan.FromSeconds(30),
                timeToLive: TimeSpan.FromMinutes(5));

            _logger.LogInformation(
                "Sent scaling notification for waiting job {JobId} in scenario {ScenarioId}",
                job.JobId, job.ScenarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send scaling notification for waiting job {JobId} in scenario {ScenarioId}",
                job.JobId, job.ScenarioId);
        }
    }

    public async Task<IEnumerable<MLJob>> GetScenarioJobsAsync(string scenarioId)
    {
        try
        {
            var jobs = new List<MLJob>();
            var jobFiles = await _storage.GetScenarioJobFilesAsync(scenarioId);

            foreach (var file in jobFiles)
            {
                try
                {
                    // result.json 파일은 건너뛰기
                    if (file.Name.EndsWith(JobResultFileName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var json = await File.ReadAllTextAsync(file.FullName);
                    var job = JsonHelper.Deserialize<MLJob>(json);

                    if (job != null)
                    {
                        job.ScenarioId = scenarioId;
                        job.JobId = Path.GetFileNameWithoutExtension(file.Name);
                        jobs.Add(job);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading job from file {FilePath}", file.FullName);
                }
            }

            return jobs.OrderByDescending(j => j.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for scenario {ScenarioId}", scenarioId);
            throw;
        }
    }

    public async Task UpdateJobStatusAsync(
        MLJob job,
        MLJobStatus newStatus,
        string? workerId = null,
        string? message = null,
        JobFailureType failureType = JobFailureType.None)
    {
        switch (newStatus)
        {
            case MLJobStatus.Running:
                if (job.WorkflowName == "default_train" && string.IsNullOrEmpty(job.ModelId))
                {
                    job.ModelId = GenerateModelId();
                    var modelPath = _storage.GetModelPath(job.ScenarioId, job.ModelId);
                    Directory.CreateDirectory(modelPath);
                    _logger.LogInformation(
                        "Created model directory for job {JobId} with ModelId {ModelId}",
                        job.JobId, job.ModelId);
                }
                job.MarkAsStarted(workerId!);
                break;

            case MLJobStatus.Completed:
                job.MarkAsCompleted();
                break;

            case MLJobStatus.Failed:
                job.MarkAsFailed(failureType, message ?? "Unknown error");
                break;

            default:
                job.Status = newStatus;
                job.AddStatusHistory(newStatus, workerId, message);
                break;
        }

        await SaveAsync(job.ScenarioId, job.JobId, job);

        _logger.LogInformation(
            "Updated job {JobId} status to {Status} in scenario {ScenarioId}. Worker: {WorkerId}, Message: {Message}",
            job.JobId, newStatus, job.ScenarioId, workerId ?? "none", message ?? "none");
    }

    public async Task SaveJobResultAsync(string scenarioId, string jobId, MLJobResult result)
    {
        try
        {
            var resultPath = _storage.GetJobResultPath(scenarioId, jobId);
            var directory = Path.GetDirectoryName(resultPath)!;
            Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(resultPath, JsonHelper.Serialize(result));

            _logger.LogInformation(
                "Saved job result for scenarioId: {ScenarioId}, jobId: {JobId}",
                scenarioId, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error saving job result for scenarioId: {ScenarioId}, jobId: {JobId}",
                scenarioId, jobId);
            throw;
        }
    }

    public async Task<MLJobResult?> GetJobResultAsync(string scenarioId, string jobId)
    {
        try
        {
            var resultPath = _storage.GetJobResultPath(scenarioId, jobId);
            if (!File.Exists(resultPath))
            {
                _logger.LogWarning(
                    "Job result file not found for scenarioId: {ScenarioId}, jobId: {JobId}",
                    scenarioId, jobId);
                return null;
            }

            return JsonHelper.Deserialize<MLJobResult>(
                await File.ReadAllTextAsync(resultPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting job result for scenarioId: {ScenarioId}, jobId: {JobId}",
                scenarioId, jobId);
            throw;
        }
    }

    private static string GenerateModelId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"m{timestamp}";
    }

    public async Task<FileStream?> AcquireJobLockAsync()
    {
        try
        {
            var lockFilePath = Path.Combine(_storage.GetScenarioBaseDir(""), "worker.lock");
            Directory.CreateDirectory(Path.GetDirectoryName(lockFilePath)!);

            return new FileStream(
                lockFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.DeleteOnClose);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public string GetJobLogsPath(string scenarioId, string jobId)
    {
        return _storage.GetJobLogsPath(scenarioId, jobId);
    }
}
