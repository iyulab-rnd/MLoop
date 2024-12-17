using Microsoft.AspNetCore.Mvc;
using MLoop.Actions;
using MLoop.Models;
using MLoop.Services;
using System.Text.Json.Nodes;

namespace MLoop.WebApp.Controllers;

[ApiController]
[Route("/api")]
[TypeFilter(typeof(GlobalExceptionFilter))]
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

    [HttpGet("scenarios/{scenarioId}")]
    public async Task<MLScenario> GetScenario(string scenarioId)
    {
        return await service.GetMLScenarioAsync(scenarioId)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioId}");
    }

    [HttpGet("scenarios/{scenarioId}/models")]
    public async IAsyncEnumerable<MLModel> GetModels(string scenarioId)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioId)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioId}");

        await foreach (var item in service.GetModelsAsync(scenario))
        {
            yield return item;
        }
    }

    [HttpPost("scenarios/{scenarioId}/train")]
    public async Task<MLTrainActionResult> BuildNewModel(string scenarioId)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioId);
        return scenario == null 
            ? throw new EntryPointNotFoundException($"Scenario not found. {scenarioId}")
            : await service.BuildNewModelAsync(scenario);
    }

    [HttpGet("scenarios/{scenarioId}/models/{modelId}")]
    public async Task<MLModel?> GetMLModel(string scenarioId, string modelId)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioId)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioId}");

        try
        {
            var mlModel = await service.GetMLModelAsync(scenario, modelId);
            return mlModel;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [HttpGet("scenarios/{scenarioId}/models/{modelId}/log")]
    public async Task<string> GetMLModelTrainLog(string scenarioId, string modelId)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioId)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioId}");

        try
        {
            var logs = await service.GetMLModelLogAsync(scenario, modelId);
            return logs;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    [HttpPost("scenarios/{scenarioId}/data")]
    public async Task<IActionResult> SubmitData(string scenarioId)
    {
        if (Request.ContentType == null)
            throw new InvalidOperationException("Required ContentType");

        var scenario = await service.GetMLScenarioAsync(scenarioId)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioId}");

        var contentType = Request.ContentType;

        if (contentType.StartsWith("application/json"))
        {
            using var reader = new StreamReader(Request.Body);
            var jsonBody = await reader.ReadToEndAsync();
            var json = JsonNode.Parse(jsonBody) ?? throw new InvalidDataException("Invalid JSON data.");
            var rowCount = await service.SubmitDataAsync(scenario, json);
            return Ok(rowCount);
        }
        else if (contentType.StartsWith("text/plain")
            || IsCsvContentType(contentType))
        {
            using var reader = new StreamReader(Request.Body);
            var csvBody = await reader.ReadToEndAsync();
            var rowCount = await service.SubmitDataAsync(scenario, csvBody);
            return Ok(rowCount);
        }
        else
        {
            return BadRequest("Unsupported Content-Type. Use 'application/json' or 'text/csv'.");
        }
    }

    [HttpPost("scenarios/{scenarioId}/predict")]
    public async Task<IActionResult> Predict(string scenarioId)
    {
        if (Request.ContentType == null)
            throw new InvalidOperationException("Required ContentType");

        var scenario = await service.GetMLScenarioAsync(scenarioId)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioId}");

        var contentType = Request.ContentType;

        if (contentType.StartsWith("application/json"))
        {
            using var reader = new StreamReader(Request.Body);
            var jsonBody = await reader.ReadToEndAsync();
            var json = JsonNode.Parse(jsonBody) ?? throw new InvalidDataException("Invalid JSON data.");
            var inputName = await service.SubmitPredictDataAsync(scenario, json);
            return Ok(inputName);
        }
        else if (contentType.StartsWith("text/plain")
            || IsCsvContentType(contentType))
        {
            using var reader = new StreamReader(Request.Body);
            var csvBody = await reader.ReadToEndAsync();
            var inputName = await service.SubmitPredictDataAsync(scenario, csvBody);
            return Ok(inputName);
        }
        else
        {
            return BadRequest("Unsupported Content-Type. Use 'application/json' or 'text/csv'.");
        }
    }

    [HttpGet("scenarios/{scenarioId}/predict/{inputName}")]
    public async Task<IActionResult> GetPredictResult(string scenarioId, string inputName)
    {
        var scenario = await service.GetMLScenarioAsync(scenarioId)
            ?? throw new EntryPointNotFoundException($"Scenario not found. {scenarioId}");

        var result = await service.GetPredictResultAsync(scenario, inputName);
        return Ok(result);
    }

    private static bool IsCsvContentType(string contentType)
    {
        var csvContentTypes = new[]
        {
            "text/csv",
            "application/csv",
            "application/x-csv",
            "text/x-csv",
            "text/comma-separated-values"
        };

        return csvContentTypes.Any(contentType.StartsWith);
    }
}
