using Microsoft.Extensions.Logging;
using MLoop.Storages;

namespace MLoop.Worker.Pipeline;

public class WorkflowContext
{
    public string ScenarioId { get; }
    public string JobId { get; }
    public string? WorkerId { get; }
    public IFileStorage Storage { get; }
    public ILogger Logger { get; }
    public Dictionary<string, object> Variables { get; }
    public CancellationToken CancellationToken { get; }

    public WorkflowContext(
        string scenarioId,
        string jobId,
        string? workerId,
        IFileStorage storage,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ScenarioId = scenarioId;
        JobId = jobId;
        WorkerId = workerId;
        Storage = storage;
        Logger = logger;
        Variables = [];
        CancellationToken = cancellationToken;
    }

    public async Task LogAsync(string message, LogLevel level = LogLevel.Information)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff UTC");
        var formattedMessage = $"[{timestamp}] {message}\n";

        Logger.Log(level, "{Message}", message);

        await FileHelper.SafeAppendAllTextAsync(
            GetJobLogsPath(),
            formattedMessage,
            CancellationToken);
    }

    // Storage 관련 경로 메서드들
    public string GetJobLogsPath()
        => Storage.GetJobLogsPath(ScenarioId, JobId);

    public string GetModelMetadataPath(string modelId)
        => Path.Combine(GetModelPath(modelId), "model.json");

    public string GetModelPath(string modelId)
        => Storage.GetModelPath(ScenarioId, modelId);

    public string GetDataPath(string fileName)
        => Path.Combine(Storage.GetScenarioDataDir(ScenarioId), fileName);

    public string GetWorkflowPath(string fileName)
        => Path.Combine(Storage.GetScenarioBaseDir(ScenarioId), "workflows", fileName);

    public string GetJobResultPath()
        => Storage.GetJobResultPath(ScenarioId, JobId);

    public string GetModelMetricsPath(string modelId)
        => Path.Combine(GetModelPath(modelId), "metrics.json");

    public string GetModelTrainLogPath(string modelId)
        => Path.Combine(GetModelPath(modelId), "train.log");

    // 헬퍼 메서드들
    public async Task EnsureModelDirectoryExistsAsync(string modelId)
    {
        var modelPath = GetModelPath(modelId);
        Directory.CreateDirectory(modelPath);
        await LogAsync($"Created model directory: {modelPath}");
    }

    public async Task CopyModelTrainLogToJobLogAsync(string modelId)
    {
        var trainLogPath = GetModelTrainLogPath(modelId);
        if (File.Exists(trainLogPath))
        {
            var trainLog = await File.ReadAllTextAsync(trainLogPath, CancellationToken);
            await LogAsync($"MLNet Training Log:\n{trainLog}");
        }
    }
}