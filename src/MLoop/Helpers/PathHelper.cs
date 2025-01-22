namespace MLoop.Helpers;

public static class PathHelper
{
    private const string SCENARIOS_DIR = "scenarios";
    private const string DATA_DIR = "data";
    private const string MODELS_DIR = "models";
    private const string WORKFLOWS_DIR = "workflows";
    private const string JOBS_DIR = "jobs";

    public static string GetScenarioPath(string path)
    {
        var (basePath, segments) = GetBasePathAndSegments(path);
        if (segments.Length < 2)
        {
            throw new ArgumentException("Invalid path format. Path must contain 'scenarios' and scenario ID.", nameof(path));
        }

        return NormalizePath(Path.Combine(basePath, SCENARIOS_DIR, segments[1]));
    }

    public static string GetDataPath(string path)
    {
        var scenarioPath = GetScenarioPath(path);
        return NormalizePath(Path.Combine(scenarioPath, DATA_DIR));
    }

    public static string GetModelsPath(string path)
    {
        var scenarioPath = GetScenarioPath(path);
        return NormalizePath(Path.Combine(scenarioPath, MODELS_DIR));
    }

    public static string GetWorkflowsPath(string path)
    {
        var scenarioPath = GetScenarioPath(path);
        return NormalizePath(Path.Combine(scenarioPath, WORKFLOWS_DIR));
    }

    public static string GetJobsPath(string path)
    {
        var scenarioPath = GetScenarioPath(path);
        return NormalizePath(Path.Combine(scenarioPath, JOBS_DIR));
    }

    private static (string BasePath, string[] Segments) GetBasePathAndSegments(string path)
    {
        var normalizedPath = NormalizePath(path);
        var scenariosIndex = normalizedPath.IndexOf($"/{SCENARIOS_DIR}/", StringComparison.OrdinalIgnoreCase);

        if (scenariosIndex == -1)
        {
            throw new ArgumentException($"Path must contain '{SCENARIOS_DIR}' directory", nameof(path));
        }

        var basePath = normalizedPath.Substring(0, scenariosIndex);
        var remainingPath = normalizedPath.Substring(scenariosIndex + 1);
        var segments = remainingPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return (basePath, segments);
    }

    public static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }

    public static string Combine(params ReadOnlySpan<string> paths)
    {
        return NormalizePath(Path.Combine(paths));
    }
}