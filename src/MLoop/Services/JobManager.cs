using Microsoft.Extensions.Logging;
using MLoop.Models.Jobs;
using MLoop.Storages;

namespace MLoop.Services;

public class JobManager
{
    private readonly IFileStorage _storage;
    private readonly ILogger<JobManager> _logger;
    private const string JobResultFileName = "result.json";

    public JobManager(IFileStorage storage, ILogger<JobManager> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    private static string GenerateModelId()
    {
        // 형식: m + yyyyMMddHHmmss
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"m{timestamp}";
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
                // Generate ModelId and create directory when moving to Running state
                if (job.JobType == MLJobType.Train && string.IsNullOrEmpty(job.ModelId))
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

        await SaveJobStatusAsync(job);

        _logger.LogInformation(
            "Updated job {JobId} status to {Status}. Worker: {WorkerId}, Message: {Message}",
            job.JobId, newStatus, workerId ?? "none", message ?? "none");
    }

    // 기본 작업 관리 메서드
    public MLJob? GetJobStatus(string scenarioId, string jobId)
    {
        try
        {
            var path = _storage.GetJobPath(scenarioId, jobId);
            if (!File.Exists(path))
            {
                _logger.LogWarning("Job file not found for scenarioId: {ScenarioId}, jobId: {JobId}",
                    scenarioId, jobId);
                return null;
            }

            var json = File.ReadAllText(path);
            return JsonHelper.Deserialize<MLJob>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status for scenarioId: {ScenarioId}, jobId: {JobId}",
                scenarioId, jobId);
            throw;
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

    public async Task SaveJobStatusAsync(MLJob job)
    {
        try
        {
            var path = _storage.GetJobPath(job.ScenarioId, job.JobId);
            var directory = Path.GetDirectoryName(path)!;

            Directory.CreateDirectory(directory);

            var json = JsonHelper.Serialize(job);
            await File.WriteAllTextAsync(path, json);

            _logger.LogInformation("Saved job status for scenarioId: {ScenarioId}, jobId: {JobId}",
                job.ScenarioId, job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving job status for scenarioId: {ScenarioId}, jobId: {JobId}",
                job.ScenarioId, job.JobId);
            throw;
        }
    }

    // 작업 결과 관리 메서드
    public async Task SaveJobResultAsync(string scenarioId, string jobId, MLJobResult result)
    {
        try
        {
            var resultPath = _storage.GetJobResultPath(scenarioId, jobId);
            var directory = Path.GetDirectoryName(resultPath)!;

            Directory.CreateDirectory(directory);

            // 결과 저장
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

            var result = JsonHelper.Deserialize<MLJobResult>(await File.ReadAllTextAsync(resultPath));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting job result for scenarioId: {ScenarioId}, jobId: {JobId}",
                scenarioId, jobId);
            throw;
        }
    }

    // Worker 관련 작업 관리 메서드
    public async Task<IEnumerable<MLJob>> GetOrphanedJobsAsync(TimeSpan threshold)
    {
        var orphanedJobs = new List<MLJob>();
        var scenarios = await _storage.GetScenarioIdsAsync();

        foreach (var scenarioId in scenarios)
        {
            var jobs = await GetScenarioJobsAsync(scenarioId);
            var potentialOrphans = jobs.Where(j =>
                j.Status == MLJobStatus.Running &&
                j.StartedAt.HasValue &&
                DateTime.UtcNow - j.StartedAt.Value > threshold);

            orphanedJobs.AddRange(potentialOrphans);
        }

        return orphanedJobs;
    }

    public async Task<MLJob?> FindAndClaimNextJobAsync(string workerId)
    {
        var lockFile = await AcquireJobLockAsync();
        if (lockFile == null) return null;

        try
        {
            // 가장 오래된 대기 중인 작업 찾기
            MLJob? oldestWaitingJob = null;
            var scenarios = await _storage.GetScenarioIdsAsync();

            foreach (var scenarioId in scenarios)
            {
                var jobs = await GetScenarioJobsAsync(scenarioId);
                var waitingJobs = jobs.Where(j => j.Status == MLJobStatus.Waiting)
                                    .OrderBy(j => j.CreatedAt);

                foreach (var job in waitingJobs)
                {
                    // 결과 파일이 이미 존재하는지 확인
                    var jobResult = await GetJobResultAsync(job.ScenarioId, job.JobId);
                    if (jobResult != null)
                    {
                        _logger.LogInformation(
                            "Job {JobId} already has a result file. Success: {Success}. Marking as {Status}.",
                            job.JobId,
                            jobResult.Success,
                            jobResult.Success ? "completed" : "failed");

                        if (jobResult.Success)
                        {
                            await UpdateJobStatusAsync(
                                job,
                                MLJobStatus.Completed,
                                workerId,
                                "Job was already processed successfully");
                        }
                        else
                        {
                            await UpdateJobStatusAsync(
                                job,
                                MLJobStatus.Failed,
                                workerId,
                                jobResult.ErrorMessage ?? "Job was already processed and failed",
                                jobResult.FailureType ?? JobFailureType.UnknownError);
                        }
                        continue;
                    }

                    // 결과 파일이 없는 가장 오래된 작업 선택
                    if (oldestWaitingJob == null || job.CreatedAt < oldestWaitingJob.CreatedAt)
                    {
                        oldestWaitingJob = job;
                        break;
                    }
                }
            }

            // 작업을 찾았다면 클레임
            if (oldestWaitingJob != null)
            {
                await UpdateJobStatusAsync(
                    oldestWaitingJob,
                    MLJobStatus.Running,
                    workerId,
                    "Job claimed by worker");

                return oldestWaitingJob;
            }

            return null;
        }
        finally
        {
            await lockFile.DisposeAsync();
        }
    }

    private async Task<FileStream?> AcquireJobLockAsync()
    {
        try
        {
            var lockFilePath = Path.Combine(_storage.GetScenarioBaseDir(""), "worker.lock");
            Directory.CreateDirectory(Path.GetDirectoryName(lockFilePath)!);

            var r = new FileStream(
                lockFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.DeleteOnClose);

            return await Task.FromResult(r);
        }
        catch (IOException)
        {
            return null;
        }
    }
}