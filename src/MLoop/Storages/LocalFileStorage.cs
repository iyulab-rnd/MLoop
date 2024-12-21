namespace MLoop.Storages;

public class LocalFileStorage : IFileStorage
{
    private readonly string basePath;

    // 경로 관련 상수 정의
    private const string ScenariosDirName = "scenarios";
    private const string DataDirName = "data";
    private const string ModelsDirName = "models";
    private const string JobsDirName = "jobs";
    private const string PredictionsDirName = "predictions";

    public LocalFileStorage(string basePath)
    {
        this.basePath = basePath;
        Directory.CreateDirectory(this.basePath);
    }

    public Task<IEnumerable<string>> GetScenarioIdsAsync()
    {
        var scenariosDir = Path.Combine(basePath, ScenariosDirName);
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
        => Path.Combine(basePath, ScenariosDirName, scenarioId);

    public string GetScenarioDataDir(string scenarioId)
        => Path.Combine(GetScenarioBaseDir(scenarioId), DataDirName);

    public string GetScenarioModelsDir(string scenarioId)
        => Path.Combine(GetScenarioBaseDir(scenarioId), ModelsDirName);

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

    public string GetScenarioJobsDir(string scenarioId)
        => Path.Combine(GetScenarioBaseDir(scenarioId), JobsDirName);

    public string GetJobPath(string scenarioId, string jobId)
        => Path.Combine(GetScenarioJobsDir(scenarioId), $"{jobId}.json");

    public string GetJobResultPath(string scenarioId, string jobId)
    => Path.Combine(GetScenarioJobsDir(scenarioId), $"{jobId}_result.json");

    public string GetJobLogsPath(string scenarioId, string jobId)
        => Path.Combine(GetScenarioJobsDir(scenarioId), $"{jobId}.log");

    public Task<IEnumerable<FileInfo>> GetScenarioJobFilesAsync(string scenarioId)
    {
        var jobsDir = GetScenarioJobsDir(scenarioId);
        if (!Directory.Exists(jobsDir))
        {
            return Task.FromResult(Enumerable.Empty<FileInfo>());
        }

        var files = Directory.GetFiles(jobsDir, "*.json", SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f));

        return Task.FromResult(files);
    }

    public string GetPredictionDir(string scenarioId, string predictionId)
        => Path.Combine(GetScenarioBaseDir(scenarioId), PredictionsDirName, predictionId);

    public string GetPredictionInputPath(string scenarioId, string predictionId, string extension)
        => Path.Combine(GetPredictionDir(scenarioId, predictionId), $"input{extension}");

    public string GetPredictionResultPath(string scenarioId, string predictionId)
        => Path.Combine(GetPredictionDir(scenarioId, predictionId), "result.csv");

    public string GetPredictionsDir(string scenarioId)
        => Path.Combine(GetScenarioBaseDir(scenarioId), PredictionsDirName);

    public Task<IEnumerable<FileInfo>> GetPredictionFiles(string scenarioId, string modelId)
    {
        var predictionsDir = Path.Combine(GetModelPath(scenarioId, modelId), PredictionsDirName);
        if (!Directory.Exists(predictionsDir))
        {
            return Task.FromResult(Enumerable.Empty<FileInfo>());
        }

        var files = Directory.GetFiles(predictionsDir, "*.*", SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f));

        return Task.FromResult(files);
    }
}
