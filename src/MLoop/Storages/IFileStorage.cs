using System.Reflection.PortableExecutable;

namespace MLoop.Storages;

public interface IFileStorage
{
    Task<IEnumerable<string>> GetScenarioIdsAsync();
    string GetScenarioBaseDir(string scenarioId);
    string GetScenarioDataDir(string scenarioId);
    string GetScenarioModelsDir(string scenarioId);
    string GetScenarioMetadataPath(string scenarioId);

    string GetModelPath(string scenarioId, string modelId);
    string GetScenarioJobsDir(string scenarioId);
    string GetJobPath(string scenarioId, string jobId);
    string GetJobResultPath(string scenarioId, string jobId);
    string GetJobLogsPath(string scenarioId, string jobId);
    Task<IEnumerable<FileInfo>> GetScenarioJobFilesAsync(string scenarioId);

    string GetPredictionDir(string scenarioId, string jobId);
    string GetPredictionInputPath(string scenarioId, string jobId, string extension);
    string GetPredictionResultPath(string scenarioId, string jobId);

    string GetPredictionsDir(string scenarioId);
    Task<IEnumerable<FileInfo>> GetPredictionFiles(string scenarioId, string modelId);

    Task<IEnumerable<DirectoryEntry>> GetScenarioDataEntriesAsync(string scenarioId, string? path = null);
    (bool isValid, string? fullPath, string? error) ValidateAndGetFullPath(string scenarioId, string relativePath);
}
