using Microsoft.Extensions.Logging;
using MLoop.Helpers;
using MLoop.Storages;

namespace MLoop.Worker.Pipeline;

public class JobContext
{
    public string ScenarioId { get; }
    public string JobId { get; }
    public string? WorkerId { get; }
    public string? ModelId { get; }
    public IFileStorage Storage { get; }
    public ILogger Logger { get; }
    public Dictionary<string, object> Variables { get; }
    public CancellationToken CancellationToken { get; }

    public JobContext(
        string scenarioId,
        string jobId,
        string? modelId,
        string? workerId,
        IFileStorage storage,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ScenarioId = scenarioId;
        JobId = jobId;
        ModelId = modelId;
        WorkerId = workerId;
        Storage = storage;
        Logger = logger;
        Variables = new Dictionary<string, object>();
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

    public string GetJobLogsPath() =>
        Storage.GetJobLogsPath(ScenarioId, JobId);

    public string GetModelPath(string modelId) =>
        Storage.GetModelPath(ScenarioId, modelId);

    public string GetDataPath(string fileName) =>
        Path.Combine(Storage.GetScenarioDataDir(ScenarioId), fileName);

    public string GetWorkflowPath(string fileName) =>
        Path.Combine(Storage.GetScenarioBaseDir(ScenarioId), "workflows", fileName);

    public string GetJobResultPath() =>
        Storage.GetJobResultPath(ScenarioId, JobId);

    // General 작업 타입을 위한 추가 메서드
    public string GetOutputPath(string fileName) =>
        Path.Combine(Storage.GetJobPath(ScenarioId, JobId), fileName);
}