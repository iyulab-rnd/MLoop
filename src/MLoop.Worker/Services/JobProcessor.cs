using Microsoft.Extensions.Logging;
using MLoop.Base;
using MLoop.Helpers;
using MLoop.Models;
using MLoop.Models.Jobs;
using MLoop.Models.Workflows;
using MLoop.Services;
using MLoop.Storages;
using MLoop.Worker.Configuration;
using MLoop.Worker.Pipeline;

namespace MLoop.Worker.Services;

public class JobProcessor : IJobProcessor
{
    private readonly ILogger<JobProcessor> _logger;
    private readonly IFileStorage _storage;
    private readonly JobManager _jobManager;
    private readonly IPipeline _pipeline;
    private readonly WorkerSettings _settings;

    private MLJob? _currentJob;
    private CancellationTokenSource? _currentJobCts;

    public JobProcessor(
        ILogger<JobProcessor> logger,
        IFileStorage storage,
        JobManager jobManager,
        IPipeline pipeline,
        WorkerSettings settings)
    {
        _logger = logger;
        _storage = storage;
        _jobManager = jobManager;
        _pipeline = pipeline;
        _settings = settings;
    }

    public async Task<bool> ProcessAsync(MLJob job, CancellationToken cancellationToken)
    {
        _currentJobCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _currentJobCts.CancelAfter(_settings.JobTimeout);
        _currentJob = job;

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["JobId"] = job.JobId,
                ["ScenarioId"] = job.ScenarioId,
                ["WorkerId"] = _settings.WorkerId
            });

            await LogJobMessageAsync(job,
                $"Job started by worker {_settings.WorkerId}\n" +
                $"Machine: {Environment.MachineName}\n" +
                $"Process ID: {Environment.ProcessId}\n" +
                $"Job Type: {job.Type}\n");

            _logger.LogInformation("Processing job {JobId} for scenario {ScenarioId}",
                job.JobId, job.ScenarioId);

            await ProcessJobByTypeAsync(job, _currentJobCts.Token);
            await HandleJobCompletionAsync(job);

            return true;
        }
        catch (OperationCanceledException) when (_currentJobCts.IsCancellationRequested)
        {
            var message = "Job was cancelled or timed out";
            _logger.LogWarning("Job {JobId} was cancelled or timed out", job.JobId);
            await LogJobMessageAsync(job, $"Error: {message}");
            await HandleJobFailureAsync(job, message, JobFailureType.Timeout);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", job.JobId);
            var (failureType, errorMessage) = ExceptionHelper.GetFailureDetails(ex);
            await LogJobMessageAsync(job, $"Error: {errorMessage}\n{ex}");
            await HandleJobFailureAsync(job, errorMessage, failureType);
            return false;
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

    public async Task<bool> CancelAsync(MLJob job)
    {
        if (_currentJob?.JobId == job.JobId)
        {
            _currentJobCts?.Cancel();
            await HandleJobFailureAsync(job, "Job cancelled by request", JobFailureType.None);
            return true;
        }
        return false;
    }

    public async Task ProcessJobByTypeAsync(MLJob job, CancellationToken cancellationToken)
    {
        var context = new JobContext(
            job.ScenarioId,
            job.JobId,
            job.GetModelId(),
            job.WorkerId,
            _storage,
            _logger,
            cancellationToken);

        try
        {
            // Job Variables 복사
            if (job.Variables != null)
            {
                foreach (var kvp in job.Variables)
                {
                    context.Variables[kvp.Key] = kvp.Value;
                }
            }

            // 모델 디렉토리 생성 (Train 작업인 경우)
            if (job.Type == JobTypes.Train && !string.IsNullOrEmpty(job.ModelId))
            {
                await EnsureModelDirectoryAsync(context, job.ModelId);
            }

            // General 작업을 위한 출력 디렉토리 생성
            if (job.Type == JobTypes.General)
            {
                await EnsureJobOutputDirectoryAsync(context);
            }

            var workflow = await LoadWorkflowAsync(job, context);
            await _pipeline.ExecuteAsync(workflow, context, cancellationToken);
        }
        catch (Exception ex) when (ex is not JobProcessException)
        {
            throw new JobProcessException(
                ExceptionHelper.GetFailureDetails(ex).failureType,
                ex.Message,
                ex);
        }
    }

    private async Task EnsureJobOutputDirectoryAsync(JobContext context)
    {
        var outputDir = Path.GetDirectoryName(context.GetJobResultPath())!;
        Directory.CreateDirectory(outputDir);
        await context.LogAsync($"Created job output directory: {outputDir}");
    }

    private async Task<Workflow> LoadWorkflowAsync(MLJob job, JobContext context)
    {
        var workflowPath = context.GetWorkflowPath($"{job.WorkflowName}.yaml");

        // 워크플로우 파일이 없는 경우 기본 워크플로우 생성
        if (!File.Exists(workflowPath))
        {
            await context.LogAsync($"No workflow file found at {workflowPath}, using default workflow");

            return job.Type switch
            {
                JobTypes.Predict => CreateDefaultPredictWorkflow(job),
                JobTypes.Train => CreateDefaultTrainWorkflow(job),
                JobTypes.General => throw new JobProcessException(
                    JobFailureType.ConfigurationError,
                    "General job type requires explicit workflow configuration"),
                _ => throw new JobProcessException(
                    JobFailureType.ConfigurationError,
                    $"Unsupported job type: {job.Type}")
            };
        }

        if (!File.Exists(workflowPath))
        {
            throw new JobProcessException(
                JobFailureType.FileNotFound,
                $"Workflow file not found: {workflowPath}");
        }

        await context.LogAsync($"Loading workflow from: {workflowPath}");
        var workflowYaml = await File.ReadAllTextAsync(workflowPath, context.CancellationToken);
        var workflow = YamlHelper.Deserialize<Workflow>(workflowYaml)
            ?? throw new JobProcessException(
                JobFailureType.ConfigurationError,
                "Failed to deserialize workflow configuration");

        await context.LogAsync(
            $"Workflow loaded successfully\n" +
            $"Number of steps: {workflow.Steps.Count}\n" +
            $"Steps: {string.Join(", ", workflow.Steps.Select(s => s.Name))}");

        return workflow;
    }

    private Workflow CreateDefaultPredictWorkflow(MLJob job) => new()
    {
        Name = job.WorkflowName,
        Type = JobTypes.Predict,
        Steps =
        [
            new WorkflowStep
            {
                Name = "predict",
                Type = "mlnet-predict",
                Config = []
            }
        ]
    };

    private Workflow CreateDefaultTrainWorkflow(MLJob job) => new()
    {
        Name = job.WorkflowName,
        Type = JobTypes.Train,
        Steps =
        [
            new WorkflowStep
        {
            Name = "train",
            Type = "mlnet-train",
            Config = new Dictionary<string, object>
            {
                ["command"] = "classification",
                ["args"] = new Dictionary<string, object>
                {
                    ["dataset"] = "train.csv",
                    ["label-col"] = "Label",
                    ["has-header"] = true
                }
            }
        }
        ]
    };

    private async Task HandleJobCompletionAsync(MLJob job)
    {
        await _jobManager.UpdateJobStatusAsync(
            job,
            MLJobStatus.Completed,
            _settings.WorkerId,
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
        await _jobManager.UpdateJobStatusAsync(
            job,
            MLJobStatus.Failed,
            _settings.WorkerId,
            errorMessage,
            failureType);

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
        if (job.Type == JobTypes.Train && !string.IsNullOrEmpty(job.ModelId))
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

    private async Task EnsureModelDirectoryAsync(JobContext context, string modelId)
    {
        var modelPath = context.GetModelPath(modelId);
        Directory.CreateDirectory(modelPath);
        await context.LogAsync($"Created model directory: {modelPath}");
    }
}