using Microsoft.Extensions.Logging;
using MLoop.Helpers;

namespace MLoop.Services;

public class ScenarioManager
{
    private readonly IFileStorage _storage;
    private readonly ILogger<ScenarioManager> _logger;

    public ScenarioManager(IFileStorage storage, ILogger<ScenarioManager> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<MLScenario?> LoadAsync(string scenarioId)
    {
        string path = _storage.GetScenarioMetadataPath(scenarioId);
        if (!File.Exists(path))
        {
            return new MLScenario
            {
                ScenarioId = scenarioId,
                Name = string.Empty,
                Tags = [],
                CreatedAt = DateTime.UtcNow
            };
        }

        var json = await File.ReadAllTextAsync(path);
        var scenario = JsonHelper.Deserialize<MLScenario>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize scenario metadata for {scenarioId}");

        scenario.ScenarioId = scenarioId;
        return scenario;
    }

    public async Task SaveAsync(string scenarioId, MLScenario scenario)
    {
        string path = _storage.GetScenarioMetadataPath(scenarioId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonHelper.Serialize(scenario);
        await File.WriteAllTextAsync(path, json);

        _logger.LogInformation("Saved scenario {ScenarioId}", scenarioId);
    }

    public async Task DeleteAsync(string scenarioId)
    {
        var scenarioDir = _storage.GetScenarioBaseDir(scenarioId);
        if (Directory.Exists(scenarioDir))
        {
            Directory.Delete(scenarioDir, recursive: true);
            _logger.LogInformation("Deleted scenario {ScenarioId} and all related files", scenarioId);
        }
    }

    public Task<bool> ExistsAsync(string scenarioId)
    {
        var path = _storage.GetScenarioMetadataPath(scenarioId);
        return Task.FromResult(File.Exists(path));
    }

    public async Task<List<MLScenario>> GetAllScenariosAsync()
    {
        var scenarios = new List<MLScenario>();
        var scenarioIds = await _storage.GetScenarioIdsAsync();

        foreach (var scenarioId in scenarioIds)
        {
            try
            {
                var scenario = await LoadAsync(scenarioId);
                if (scenario != null)
                {
                    scenarios.Add(scenario);
                    _logger.LogInformation("Loaded scenario: {ScenarioId}", scenarioId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading scenario {ScenarioId}", scenarioId);
            }
        }

        return scenarios;
    }
}
