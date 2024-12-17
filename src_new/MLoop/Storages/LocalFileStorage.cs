namespace MLoop.Storages;

public class LocalFileStorage : IFileStorage
{
    private readonly string basePath;

    public LocalFileStorage(string basePath)
    {
        this.basePath = basePath;
        Directory.CreateDirectory(this.basePath);
    }

    public Task<IEnumerable<string>> GetScenarioIdsAsync()
    {
        var scenariosDir = Path.Combine(basePath, "scenarios");
        if (!Directory.Exists(scenariosDir))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        return Task.FromResult(
            Directory.GetDirectories(scenariosDir)
                .Select(path => Path.GetFileName(path))
                .Where(name => !string.IsNullOrEmpty(name))!
        );
    }

    public string GetScenarioBaseDir(string scenarioId)
        => Path.Combine(basePath, "scenarios", scenarioId);

    public string GetScenarioDataDir(string scenarioId)
        => Path.Combine(GetScenarioBaseDir(scenarioId), "data");

    public string GetJobPath(string jobId)
        => Path.Combine(basePath, "jobs", $"{jobId}.json");

    public string GetJobResultPath(string jobId)
        => Path.Combine(basePath, "jobs", $"{jobId}_result.json");

    public string GetScenarioModelsDir(string scenarioId)
        => Path.Combine(GetScenarioBaseDir(scenarioId), "models");

    public string GetModelPath(string scenarioId, string modelId)
        => Path.Combine(GetScenarioModelsDir(scenarioId), modelId);

    public string GetScenarioMetadataPath(string scenarioId)
        => Path.Combine(GetScenarioBaseDir(scenarioId), "scenario.json");

    public Task<IEnumerable<FileInfo>> GetScenarioDataFilesAsync(string scenarioId)
    {
        var dataDir = GetScenarioDataDir(scenarioId);
        if (!Directory.Exists(dataDir))
        {
            return Task.FromResult(Enumerable.Empty<FileInfo>());
        }

        var files = Directory.GetFiles(dataDir, "*.*", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f));

        return Task.FromResult(files);
    }
}