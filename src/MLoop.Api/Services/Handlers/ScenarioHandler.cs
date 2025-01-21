using MLoop.Api.Models.Scenarios;
using MLoop.Base;
using MLoop.Models;
using MLoop.Services;

namespace MLoop.Api.Services.Handlers;

public class ScenarioHandler
{
    private readonly ScenarioManager _scenarioManager;
    private readonly ILogger<ScenarioHandler> _logger;

    private static readonly string[] ValidMLTypes =
    {
        "classification",
        "regression",
        "recommendation",
        "image-classification",
        "text-classification",
        "forecasting",
        "object-detection"
    };

    public ScenarioHandler(
        ScenarioManager scenarioManager,
        ILogger<ScenarioHandler> logger)
    {
        _scenarioManager = scenarioManager;
        _logger = logger;
    }

    public async Task<MLScenario> ProcessAsync(MLScenario scenario)
    {
        await _scenarioManager.SaveAsync(scenario.ScenarioId, scenario);
        return scenario;
    }

    public void ValidateScenario(MLScenario scenario)
    {
        if (string.IsNullOrEmpty(scenario.ScenarioId))
            throw new ValidationException("ScenarioId is required");

        if (string.IsNullOrEmpty(scenario.Name))
            throw new ValidationException("Name is required");

        if (string.IsNullOrEmpty(scenario.MLType))
            throw new ValidationException("MLType is required");

        if (!ValidMLTypes.Contains(scenario.MLType.ToLower()))
            throw new ValidationException($"Invalid MLType: {scenario.MLType}. Valid types are: {string.Join(", ", ValidMLTypes)}");

        if (scenario.Tags != null && scenario.Tags.Count > 10)
            throw new ValidationException("Maximum 10 tags are allowed");
    }

    public async Task<MLScenario> InitializeScenarioAsync(CreateScenarioRequest request)
    {
        var scenario = new MLScenario
        {
            ScenarioId = Guid.NewGuid().ToString("N"),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            MLType = request.MLType.Trim().ToLower(),
            Tags = request.Tags?.Select(t => t.Trim().ToLower()).ToList() ?? [],
            CreatedAt = DateTime.UtcNow
        };

        ValidateScenario(scenario);
        return await ProcessAsync(scenario);
    }

    public async Task<MLScenario> UpdateScenarioAsync(string scenarioId, UpdateScenarioRequest request)
    {
        var scenario = await _scenarioManager.LoadAsync(scenarioId)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found");

        if (!string.IsNullOrEmpty(request.Name))
            scenario.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.MLType))
            scenario.MLType = request.MLType.Trim().ToLower();

        if (request.Description != null)
            scenario.Description = request.Description.Trim();

        if (request.Tags != null)
            scenario.Tags = request.Tags.Select(t => t.Trim().ToLower()).ToList();

        ValidateScenario(scenario);
        return await ProcessAsync(scenario);
    }
}
