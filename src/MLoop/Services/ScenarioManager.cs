using System.Text.Json;
using Microsoft.Extensions.Logging;
using MLoop.Models;
using MLoop.Storages;

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

    public async Task<List<ScenarioMetadata>> GetAllScenariosAsync()
    {
        var scenarios = new List<ScenarioMetadata>();
        var scenarioIds = await _storage.GetScenarioIdsAsync();

        foreach (var scenarioId in scenarioIds)
        {
            try
            {
                var scenario = await LoadScenarioAsync(scenarioId);
                scenarios.Add(scenario);
                _logger.LogInformation("Successfully loaded scenario: {ScenarioId}", scenarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading scenario {ScenarioId}", scenarioId);
            }
        }

        return scenarios;
    }

    public async Task<ScenarioMetadata> LoadScenarioAsync(string scenarioId)
    {
        string path = _storage.GetScenarioMetadataPath(scenarioId);
        if (!File.Exists(path))
        {
            return new ScenarioMetadata
            {
                ScenarioId = scenarioId,
                Name = string.Empty,
                Tags = [],
                CreatedAt = DateTime.UtcNow
            };
        }

        var json = await File.ReadAllTextAsync(path);
        return JsonHelper.Deserialize<ScenarioMetadata>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize scenario metadata for {scenarioId}");
    }

    public async Task SaveScenarioAsync(string scenarioId, ScenarioMetadata metadata)
    {
        string path = _storage.GetScenarioMetadataPath(scenarioId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonHelper.Serialize(metadata);
        await File.WriteAllTextAsync(path, json);
    }

    public Task DeleteScenarioAsync(string scenarioId)
    {
        var scenarioDir = _storage.GetScenarioBaseDir(scenarioId);
        if (Directory.Exists(scenarioDir))
        {
            Directory.Delete(scenarioDir, recursive: true);
        }
        return Task.CompletedTask;
    }
}