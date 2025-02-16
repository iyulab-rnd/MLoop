using System.Reflection.PortableExecutable;

namespace MLoop.Storages;

public interface IFileStorage
{
    string GetDatasetsBaseDir();
    string GetDatasetPath(string name);
    string GetDatasetDataDir(string name);
    string GetDatasetMetadataPath(string name);
    Task<IEnumerable<string>> GetDatasetNamesAsync();
    Task<IEnumerable<DirectoryEntry>> GetDatasetEntriesAsync(string name, string? path = null);
    (bool isValid, string? fullPath, string? error) ValidateDatasetPath(string name, string relativePath);

    Task<IEnumerable<string>> GetScenarioIdsAsync();
    string GetScenarioBaseDir(string scenarioId);
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
}
