namespace MLoop.Storages;

public class LocalFileStorage : IFileStorage
{
    private readonly string basePath;

    private const string DatasetsDirName = "datasets";
    private const string DataDirName = "data";

    private const string ScenariosDirName = "scenarios";
    private const string ModelsDirName = "models";
    private const string JobsDirName = "jobs";
    private const string PredictionsDirName = "predictions";

    public LocalFileStorage(string basePath)
    {
        this.basePath = basePath;
        Directory.CreateDirectory(this.basePath);
    }

    #region Dataset

    public string GetDatasetsBaseDir()
    => PathHelper.Combine(basePath, DatasetsDirName);

    public string GetDatasetPath(string name)
        => PathHelper.Combine(GetDatasetsBaseDir(), name);

    public string GetDatasetDataDir(string name)
        => PathHelper.Combine(GetDatasetPath(name), DataDirName);

    public string GetDatasetMetadataPath(string name)
        => PathHelper.Combine(GetDatasetPath(name), "dataset.json");

    public Task<IEnumerable<string>> GetDatasetNamesAsync()
    {
        var datasetsDir = GetDatasetsBaseDir();
        if (!Directory.Exists(datasetsDir))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        return Task.FromResult(
            Directory.GetDirectories(datasetsDir)
                .Select(path => Path.GetFileName(path))
                .Where(name => !string.IsNullOrEmpty(name))!
        );
    }

    public async Task<IEnumerable<DirectoryEntry>> GetDatasetEntriesAsync(string name, string? path = null)
    {
        var entries = new List<DirectoryEntry>();
        var dataDir = GetDatasetDataDir(name);
        var targetDir = dataDir;

        if (!string.IsNullOrEmpty(path))
        {
            var validationResult = ValidateDatasetPath(name, path);
            if (!validationResult.isValid)
            {
                throw new ArgumentException(validationResult.error);
            }
            targetDir = validationResult.fullPath!;
        }

        if (!Directory.Exists(targetDir))
        {
            return entries;
        }

        await Task.Run(() =>
        {
            var dirInfo = new DirectoryInfo(targetDir);
            foreach (var dir in dirInfo.GetDirectories())
            {
                entries.Add(new DirectoryEntry(
                    dir.Name,
                    IOHelper.NormalizePath(Path.GetRelativePath(dataDir, dir.FullName)),
                    0,
                    dir.LastWriteTimeUtc,
                    true
                ));
            }

            foreach (var file in dirInfo.GetFiles())
            {
                entries.Add(new DirectoryEntry(
                    file.Name,
                    IOHelper.NormalizePath(Path.GetRelativePath(dataDir, file.FullName)),
                    file.Length,
                    file.LastWriteTimeUtc,
                    false
                ));
            }
        });

        return entries;
    }

    public (bool isValid, string? fullPath, string? error) ValidateDatasetPath(string name, string relativePath)
    {
        if (relativePath.Contains("..") || Path.IsPathRooted(relativePath))
        {
            return (false, null, "Invalid file path.");
        }

        var dataDir = GetDatasetDataDir(name);
        var fullPath = PathHelper.Combine(dataDir, relativePath);

        if (!Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(dataDir)))
        {
            return (false, null, "Invalid file path.");
        }

        return (true, fullPath, null);
    }

    #endregion

    #region Scenario

    public Task<IEnumerable<string>> GetScenarioIdsAsync()
    {
        var scenariosDir = PathHelper.Combine(basePath, ScenariosDirName);
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
        => PathHelper.Combine(basePath, ScenariosDirName, scenarioId);

    public string GetScenarioModelsDir(string scenarioId)
        => PathHelper.Combine(GetScenarioBaseDir(scenarioId), ModelsDirName);

    public string GetModelPath(string scenarioId, string modelId)
        => PathHelper.Combine(GetScenarioModelsDir(scenarioId), modelId);

    public string GetScenarioMetadataPath(string scenarioId)
        => PathHelper.Combine(GetScenarioBaseDir(scenarioId), "scenario.json");

    public string GetScenarioJobsDir(string scenarioId)
        => PathHelper.Combine(GetScenarioBaseDir(scenarioId), JobsDirName);

    public string GetJobPath(string scenarioId, string jobId)
        => PathHelper.Combine(GetScenarioJobsDir(scenarioId), $"{jobId}.json");

    public string GetJobResultPath(string scenarioId, string jobId)
        => PathHelper.Combine(GetScenarioJobsDir(scenarioId), $"{jobId}_result.json");

    public string GetJobLogsPath(string scenarioId, string jobId)
        => PathHelper.Combine(GetScenarioJobsDir(scenarioId), $"{jobId}.log");

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

    public string GetPredictionDir(string scenarioId, string jobId)
        => PathHelper.Combine(GetScenarioBaseDir(scenarioId), PredictionsDirName, jobId);

    public string GetPredictionInputPath(string scenarioId, string jobId, string extension)
        => PathHelper.Combine(GetPredictionDir(scenarioId, jobId), $"input{extension}");

    public string GetPredictionResultPath(string scenarioId, string jobId)
        => PathHelper.Combine(GetPredictionDir(scenarioId, jobId), "result.csv");

    public string GetPredictionsDir(string scenarioId)
        => PathHelper.Combine(GetScenarioBaseDir(scenarioId), PredictionsDirName);

    public Task<IEnumerable<FileInfo>> GetPredictionFiles(string scenarioId, string modelId)
    {
        var predictionsDir = PathHelper.Combine(GetModelPath(scenarioId, modelId), PredictionsDirName);
        if (!Directory.Exists(predictionsDir))
        {
            return Task.FromResult(Enumerable.Empty<FileInfo>());
        }

        var files = Directory.GetFiles(predictionsDir, "*.*", SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f));

        return Task.FromResult(files);
    }

    #endregion
}
