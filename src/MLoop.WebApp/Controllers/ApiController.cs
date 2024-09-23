using Microsoft.AspNetCore.Mvc;
using MLoop.Actions;
using MLoop.BackgroundServices;
using MLoop.Models;
using MLoop.Services;

namespace MLoop.WebApp.Controllers;

[ApiController]
[Route("/api")]
public class ApiController : ControllerBase
{
    private readonly MLoopApiService service;

    public ApiController(MLoopApiService service)
    {
        this.service = service;
    }

    [HttpGet("scenarios")]
    public IAsyncEnumerable<MLScenario> GetScenarios()
    {
        return service.GetMLScenariosAsync();
    }

    [HttpGet("scenarios/{scenarioName}")]
    public async Task<MLScenario> GetScenario(string scenarioName)
    {
        return await service.GetMLScenarioAsync(scenarioName)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioName}");
    }

    [HttpGet("scenarios/{scenarioName}/models")]
    public async IAsyncEnumerable<MLModel> GetModels(string scenarioName)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioName)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioName}");

        await foreach (var item in service.GetModelsAsync(scenario))
        {
            yield return item;
        }
    }

    [HttpPost("scenarios/{scenarioName}/new-train")]
    public async Task<MLTrainActionResult> BuildNewModel(string scenarioName)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioName);
        return scenario == null 
            ? throw new EntryPointNotFoundException($"Scenario not found. {scenarioName}")
            : await service.BuildNewModelAsync(scenario);
    }

    [HttpGet("scenarios/{scenarioName}/models/{modelName}")]
    public async Task<MLModel?> GetMLModel(string scenarioName, string modelName)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioName)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioName}");

        try
        {
            var mlModel = await service.GetMLModelAsync(scenario, modelName);
            return mlModel;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [HttpGet("scenarios/{scenarioName}/models/{modelName}/log")]
    public async Task<string> GetMLModelTrainLog(string scenarioName, string modelName)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioName)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioName}");

        try
        {
            var logs = await service.GetMLModelLogAsync(scenario, modelName);
            return logs;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}