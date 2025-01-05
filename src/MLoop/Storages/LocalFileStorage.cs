namespace MLoop.Storages;

public class LocalFileStorage : IFileStorage
{
    private readonly string basePath;

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

    private string NormalizePath(string path)
        => path.Replace("\\", "/");

    public (bool isValid, string? fullPath, string? error) ValidateAndGetFullPath(string scenarioId, string relativePath)
    {
        if (relativePath.Contains("..") || Path.IsPathRooted(relativePath))
        {
            return (false, null, "Invalid file path.");
        }

        var dataDir = GetScenarioDataDir(scenarioId);
        var fullPath = Path.Combine(dataDir, relativePath);

        // Prevent directory traversal
        if (!Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(dataDir)))
        {
            return (false, null, "Invalid file path.");
        }

        return (true, fullPath, null);
    }

    public async Task<IEnumerable<DirectoryEntry>> GetScenarioDataEntriesAsync(string scenarioId, string? path = null)
    {
        var entries = new List<DirectoryEntry>();
        var dataDir = GetScenarioDataDir(scenarioId);
        var targetDir = dataDir;

        // 경로가 제공된 경우 유효성 검사 및 전체 경로 구성
        if (!string.IsNullOrEmpty(path))
        {
            var validationResult = ValidateAndGetFullPath(scenarioId, path);
            if (!validationResult.isValid)
            {
                throw new ArgumentException(validationResult.error);
            }
            targetDir = validationResult.fullPath!;
        }

        // 디렉토리가 존재하지 않는 경우 빈 목록 반환
        if (!Directory.Exists(targetDir))
        {
            return entries;
        }

        await Task.Run(() =>
        {
            var dirInfo = new DirectoryInfo(targetDir);

            // 디렉토리 항목 추가
            foreach (var dir in dirInfo.GetDirectories())
            {
                entries.Add(new DirectoryEntry(
                    name: dir.Name,
                    path: NormalizePath(Path.GetRelativePath(dataDir, dir.FullName)),
                    size: 0,
                    lastModified: dir.LastWriteTimeUtc,
                    isDirectory: true
                ));
            }

            // 파일 항목 추가
            foreach (var file in dirInfo.GetFiles())
            {
                entries.Add(new DirectoryEntry(
                    name: file.Name,
                    path: NormalizePath(Path.GetRelativePath(dataDir, file.FullName)),
                    size: file.Length,
                    lastModified: file.LastWriteTimeUtc,
                    isDirectory: false
                ));
            }
        });

        return entries;
    }
}
