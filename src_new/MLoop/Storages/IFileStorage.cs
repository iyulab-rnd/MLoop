namespace MLoop.Storages;

public interface IFileStorage
{
    Task<IEnumerable<string>> GetScenarioIdsAsync();
    string GetScenarioBaseDir(string scenarioId);
    string GetScenarioDataDir(string scenarioId);

    string GetScenarioModelsDir(string scenarioId);
    string GetModelPath(string scenarioId, string modelId);

    string GetScenarioMetadataPath(string scenarioId);
    Task<IEnumerable<FileInfo>> GetScenarioDataFilesAsync(string scenarioId);

    string GetJobPath(string jobId);
    string GetJobResultPath(string jobId);
}