using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Models.Jobs;
using MLoop.Services;
using MLoop.Storages;
using MLoop.Worker.Configuration;

namespace MLoop.Worker.Services;

public class WorkerService : BackgroundService
{
    private readonly string _workerId;
    private readonly IFileStorage _storage;
    private readonly JobManager _jobManager;
    private readonly JobProcessor _jobProcessor;
    private readonly ILogger<WorkerService> _logger;
    private readonly WorkerSettings _settings;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly bool _hasQueue;
    private DateTime _lastJobCheckTime;
    private FileSystemWatcher? _scenarioWatcher;

    public WorkerService(
        IOptions<WorkerSettings> settings,
        IFileStorage storage,
        JobManager jobManager,
        JobProcessor jobProcessor,
        ILogger<WorkerService> logger,
        IConfiguration configuration,
        IHostApplicationLifetime hostLifetime)
    {
        _settings = settings.Value;
        _storage = storage;
        _jobManager = jobManager;
        _jobProcessor = jobProcessor;
        _logger = logger;
        _hostLifetime = hostLifetime;
        _workerId = _settings.WorkerId ?? $"worker_{Environment.MachineName}_{Guid.NewGuid():N}";
        _lastJobCheckTime = DateTime.UtcNow;

        var queueConnection = configuration.GetConnectionString("QueueConnection");
        _hasQueue = !string.IsNullOrEmpty(queueConnection);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            """
            Worker service starting:
            WorkerId: {WorkerId}
            Machine: {MachineName}
            Process: {ProcessId}
            Base Directory: {BaseDir}
            Job Timeout: {JobTimeout}
            Job Polling Interval: {PollingInterval}
            Idle Timeout: {IdleTimeout}
            """,
            _workerId,
            Environment.MachineName,
            Environment.ProcessId,
            _storage.GetScenarioBaseDir(""),
            _settings.JobTimeout,
            _settings.JobPollingInterval,
            _settings.IdleTimeout);

        try
        {
            InitializeFileSystemWatcher();
            await RecoverAbandonedJobs(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during worker startup");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextJob = await FindNextJobWithBackoff(stoppingToken);
                if (nextJob != null)
                {
                    _lastJobCheckTime = DateTime.UtcNow;
                    await _jobProcessor.ProcessAsync(nextJob, stoppingToken);
                }
                else if (ShouldShutdown())
                {
                    _logger.LogInformation(
                        "Worker idle timeout reached after {IdleMinutes} minutes. Shutting down.",
                        _settings.IdleTimeout.TotalMinutes);

                    _hostLifetime.StopApplication();
                    break;
                }

                await Task.Delay(_settings.JobPollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop");
                await Task.Delay(_settings.JobPollingInterval, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker service stopping: {WorkerId}", _workerId);

        _scenarioWatcher?.Dispose();

        await base.StopAsync(cancellationToken);
    }

    private void InitializeFileSystemWatcher()
    {
        var baseDir = _storage.GetScenarioBaseDir("");
        _scenarioWatcher = new FileSystemWatcher(baseDir)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _scenarioWatcher.Created += OnScenarioCreated;
        _scenarioWatcher.Changed += OnScenarioChanged;

        _logger.LogInformation("Initialized file system watcher for directory: {Directory}", baseDir);
    }

    private void OnScenarioCreated(object sender, FileSystemEventArgs e)
    {
        try
        {
            var scenarioId = GetScenarioIdFromPath(e.FullPath);
            if (string.IsNullOrEmpty(scenarioId)) return;

            _logger.LogInformation("New scenario detected: {ScenarioId}", scenarioId);
            _lastJobCheckTime = DateTime.UtcNow; // Reset idle timeout
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling new scenario creation: {Path}", e.FullPath);
        }
    }

    private void OnScenarioChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            var scenarioId = GetScenarioIdFromPath(e.FullPath);
            if (string.IsNullOrEmpty(scenarioId)) return;

            _logger.LogDebug("Scenario changes detected: {ScenarioId}", scenarioId);
            _lastJobCheckTime = DateTime.UtcNow; // Reset idle timeout
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling scenario changes: {Path}", e.FullPath);
        }
    }

    private string? GetScenarioIdFromPath(string path)
    {
        var baseDir = _storage.GetScenarioBaseDir("");
        if (!path.StartsWith(baseDir)) return null;

        var relativePath = path[baseDir.Length..].TrimStart(Path.DirectorySeparatorChar);
        var parts = relativePath.Split(Path.DirectorySeparatorChar);

        return parts.Length > 0 ? parts[0] : null;
    }

    private async Task<MLJob?> FindNextJobWithBackoff(CancellationToken stoppingToken)
    {
        var backoffInterval = TimeSpan.FromSeconds(1);
        var maxInterval = _settings.JobPollingInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await _jobManager.FindAndClaimNextJobAsync(_workerId);
            if (job != null)
            {
                _logger.LogInformation(
                    "Found and claimed job {JobId} for scenario {ScenarioId}",
                    job.JobId, job.ScenarioId);
                return job;
            }

            await Task.Delay(backoffInterval, stoppingToken);
            backoffInterval = TimeSpan.FromTicks(Math.Min(
                backoffInterval.Ticks * 2,
                maxInterval.Ticks
            ));
        }

        return null;
    }

    private async Task RecoverAbandonedJobs(CancellationToken cancellationToken)
    {
        var scenarios = await _storage.GetScenarioIdsAsync();
        if (scenarios.Any())
        {
            _logger.LogInformation(
                "Found {Count} scenarios: {Scenarios}",
                scenarios.Count(),
                string.Join(", ", scenarios));
        }
        else
        {
            _logger.LogInformation("No scenarios found in base directory");
            return;
        }

        foreach (var scenarioId in scenarios)
        {
            try
            {
                var jobs = await _jobManager.GetScenarioJobsAsync(scenarioId);
                var abandonedJobs = jobs.Where(j =>
                    j.Status == MLJobStatus.Running &&
                    j.WorkerId == _workerId);

                foreach (var job in abandonedJobs)
                {
                    _logger.LogWarning(
                        "Found abandoned job from previous worker instance - JobId: {JobId}, ScenarioId: {ScenarioId}",
                        job.JobId, job.ScenarioId);

                    await _jobManager.UpdateJobStatusAsync(
                        job,
                        MLJobStatus.Failed,
                        _workerId,
                        "Job was abandoned due to worker restart",
                        JobFailureType.WorkerCrash);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error recovering abandoned jobs for scenario {ScenarioId}",
                    scenarioId);
            }
        }
    }

    private bool ShouldShutdown()
    {
        return _hasQueue &&
               DateTime.UtcNow - _lastJobCheckTime > _settings.IdleTimeout;
    }
}