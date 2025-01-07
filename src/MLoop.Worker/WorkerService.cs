using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Models.Jobs;
using MLoop.Services;
using MLoop.Worker.Configuration;
using MLoop.Worker.Pipeline;
using Microsoft.Extensions.Configuration;
using MLoop.Storages;
using MLoop.Models.Workflows;

namespace MLoop.Worker;

public class WorkerService : BackgroundService
{
    private readonly string _workerId;
    private readonly IFileStorage _storage;
    private readonly JobManager _jobManager;
    private readonly PipelineExecutor _pipelineExecutor;
    private readonly ILogger<WorkerService> _logger;
    private readonly WorkerSettings _settings;
    private MLJob? _currentJob;
    private CancellationTokenSource? _currentJobCts;
    private DateTime _lastJobCheckTime;
    private readonly IHostApplicationLifetime _hostLifetime;
    private bool hasQueue;

    public WorkerService(
        IOptions<WorkerSettings> settings,
        IFileStorage storage,
        JobManager jobManager,
        PipelineExecutor pipelineExecutor,
        ILogger<WorkerService> logger,
        IConfiguration configuration,
        IHostApplicationLifetime hostLifetime)
    {
        _settings = settings.Value;
        _storage = storage;
        _jobManager = jobManager;
        _pipelineExecutor = pipelineExecutor;
        _logger = logger;
        _workerId = _settings.WorkerId ?? $"worker_{Environment.MachineName}_{Guid.NewGuid():N}";
        _lastJobCheckTime = DateTime.UtcNow;
        _hostLifetime = hostLifetime;

        var queueConnection = configuration.GetConnectionString("QueueConnection");
        this.hasQueue = !string.IsNullOrEmpty(queueConnection);
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
            }

            foreach (var scenarioId in scenarios)
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

                    await HandleJobFailureAsync(
                        job,
                        "Job was abandoned due to worker restart or crash",
                        JobFailureType.WorkerCrash);
                }
            }
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
                if (_currentJob == null)
                {
                    var nextJob = await _jobManager.FindAndClaimNextJobAsync(_workerId);
                    if (nextJob != null)
                    {
                        _lastJobCheckTime = DateTime.UtcNow;
                        await ProcessJobAsync(nextJob, stoppingToken);
                    }
                    else if (hasQueue && DateTime.UtcNow - _lastJobCheckTime > _settings.IdleTimeout)
                    {
                        _logger.LogInformation(
                            "Worker idle timeout reached after {IdleMinutes} minutes. Shutting down.",
                            _settings.IdleTimeout.TotalMinutes);

                        // 호스트 종료 요청
                        _hostLifetime.StopApplication();
                        break;
                    }
                    await Task.Delay(_settings.JobPollingInterval, stoppingToken);
                }
                else
                {
                    await Task.Delay(_settings.JobPollingInterval, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop");
                await Task.Delay(_settings.JobPollingInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(MLJob job, CancellationToken stoppingToken)
    {
        _currentJobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _currentJobCts.CancelAfter(_settings.JobTimeout);
        _currentJob = job;

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["JobId"] = job.JobId,
                ["ScenarioId"] = job.ScenarioId,
                ["WorkerId"] = _workerId
            });

            await LogJobMessageAsync(job,
                $"Job started by worker {_workerId}\n" +
                $"Machine: {Environment.MachineName}\n" +
                $"Process ID: {Environment.ProcessId}\n" +
                $"Job Type: {job.JobType}\n");

            _logger.LogInformation("Processing job {JobId} for scenario {ScenarioId}",
                job.JobId, job.ScenarioId);

            await ProcessJobByTypeAsync(job, _currentJobCts.Token);
            await HandleJobCompletionAsync(job);
        }
        catch (OperationCanceledException) when (_currentJobCts.IsCancellationRequested)
        {
            var message = "Job was cancelled or timed out";
            _logger.LogWarning("Job {JobId} was cancelled or timed out", job.JobId);
            await LogJobMessageAsync(job, $"Error: {message}");
            await HandleJobFailureAsync(job, message, JobFailureType.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", job.JobId);
            var (failureType, errorMessage) = ExceptionHelper.GetFailureDetails(ex);
            await LogJobMessageAsync(job, $"Error: {errorMessage}\n{ex}");
            await HandleJobFailureAsync(job, errorMessage, failureType);
        }
        finally
        {
            _currentJob = null;
            if (_currentJobCts != null)
            {
                _currentJobCts.Cancel();
                _currentJobCts.Dispose();
                _currentJobCts = null;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker service stopping: {WorkerId}", _workerId);

        if (_currentJob != null)
        {
            await HandleJobFailureAsync(
                _currentJob,
                "Worker shutdown initiated",
                JobFailureType.WorkerCrash);
        }

        if (_currentJobCts != null)
        {
            _currentJobCts.Cancel();
            _currentJobCts.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task LogJobMessageAsync(MLJob job, string message)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff UTC");
            var logMessage = $"[{timestamp}] {message}\n";
            await File.AppendAllTextAsync(_storage.GetJobLogsPath(job.ScenarioId, job.JobId), logMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to job log file");
        }
    }

    private async Task HandleJobCompletionAsync(MLJob job)
    {
        await _jobManager.UpdateJobStatusAsync(
            job,
            MLJobStatus.Completed,
            _workerId,
            "Job completed successfully");

        await LogJobMessageAsync(job, "Job completed successfully");

        var result = new MLJobResult
        {
            Success = true,
            CompletedAt = DateTime.UtcNow,
            Metrics = new Dictionary<string, object>
            {
                ["duration_ms"] = (DateTime.UtcNow - (job.StartedAt ?? DateTime.UtcNow)).TotalMilliseconds
            }
        };

        await _jobManager.SaveJobResultAsync(job.ScenarioId, job.JobId, result);
    }

    private async Task HandleJobFailureAsync(MLJob job, string errorMessage, JobFailureType failureType)
    {
        // 작업 상태 업데이트
        await _jobManager.UpdateJobStatusAsync(
            job,
            MLJobStatus.Failed,
            _workerId,
            errorMessage,
            failureType);

        // 실패 결과 저장
        var result = new MLJobResult
        {
            Success = false,
            CompletedAt = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            FailureType = failureType,
            Metrics = new Dictionary<string, object>
            {
                ["duration_ms"] = (DateTime.UtcNow - (job.StartedAt ?? DateTime.UtcNow)).TotalMilliseconds,
                ["error_type"] = failureType.ToString()
            }
        };

        await _jobManager.SaveJobResultAsync(job.ScenarioId, job.JobId, result);

        // 훈련 작업인 경우에만 모델 디렉토리 삭제
        if (job.JobType == MLJobType.Train && !string.IsNullOrEmpty(job.ModelId))
        {
            try
            {
                var modelDir = _storage.GetModelPath(job.ScenarioId, job.ModelId);
                if (Directory.Exists(modelDir))
                {
                    Directory.Delete(modelDir, recursive: true);
                    _logger.LogInformation(
                        "Cleaned up model directory for failed training job {JobId}, ModelId: {ModelId}",
                        job.JobId, job.ModelId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to cleanup model directory for training job {JobId}, ModelId: {ModelId}",
                    job.JobId, job.ModelId);
            }
        }
    }

    private async Task ProcessJobByTypeAsync(MLJob job, CancellationToken cancellationToken)
    {
        var context = new WorkflowContext(
            job.ScenarioId,
            job.JobId,
            job.WorkerId,
            _storage,
            _logger,
            cancellationToken);

        try
        {
            // ModelId를 context에 추가
            if (!string.IsNullOrEmpty(job.ModelId))
            {
                context.Variables["ModelId"] = job.ModelId;
            }

            // Job Variables 복사
            if (job.Variables != null)
            {
                foreach (var kvp in job.Variables)
                {
                    context.Variables[kvp.Key] = kvp.Value;
                }
            }

            if (job.JobType == MLJobType.Train)
            {
                // 모델 디렉토리 생성 (Train 작업인 경우)
                await context.EnsureModelDirectoryExistsAsync(job.ModelId!);
            }

            var workflow = await LoadWorkflowAsync(job, context);
            await _pipelineExecutor.ExecuteAsync(workflow, context, cancellationToken);
        }
        catch (Exception ex) when (ex is not JobProcessException)
        {
            throw new JobProcessException(
                ExceptionHelper.GetFailureDetails(ex).failureType,
                ex.Message,
                ex);
        }
    }

    private async Task<WorkflowConfig> LoadWorkflowAsync(MLJob job, WorkflowContext context)
    {
        var workflowPath = context.GetWorkflowPath(job.JobType switch
        {
            MLJobType.Train => "train.yaml",
            MLJobType.Predict => "predict.yaml",
            _ => throw new NotSupportedException($"Unsupported job type: {job.JobType}")
        });

        // 예측 작업이고 워크플로우 파일이 없는 경우 기본 워크플로우 생성
        if (job.JobType == MLJobType.Predict && !File.Exists(workflowPath))
        {
            await context.LogAsync("No prediction workflow found, using default prediction step");

            return new WorkflowConfig
            {
                Steps =
            {
                new WorkflowStepConfig
                {
                    Name = "predict",
                    Type = "mlnet-predict",
                    Config = new Dictionary<string, object>
                    {
                        ["model-id"] = job.ModelId ?? throw new InvalidOperationException("ModelId is required for prediction")
                    }
                }
            }
            };
        }

        if (!File.Exists(workflowPath))
        {
            var msg = $"Workflow file not found: {workflowPath}";
            throw new JobProcessException(JobFailureType.FileNotFound, msg);
        }

        await context.LogAsync($"Loading workflow from: {workflowPath}");
        var workflowYaml = await File.ReadAllTextAsync(workflowPath, context.CancellationToken);
        var workflow = YamlHelper.Deserialize<WorkflowConfig>(workflowYaml)
            ?? throw new JobProcessException(JobFailureType.ConfigurationError, "Failed to deserialize workflow configuration");

        await context.LogAsync(
            $"Workflow loaded successfully\n" +
            $"Number of steps: {workflow.Steps.Count}\n" +
            $"Steps: {string.Join(", ", workflow.Steps.Select(s => s.Name))}");

        return workflow;
    }
}