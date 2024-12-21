using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.OData.Query;
using MLoop.Api.Models;
using MLoop.Api.Services;
using MLoop.Models;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ScenariosController : ControllerBase
{
    private readonly ScenarioService _scenarioService;
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(
        ScenarioService scenarioService,
        ILogger<ScenariosController> logger)
    {
        _scenarioService = scenarioService;
        _logger = logger;
    }

    [HttpGet]
    [EnableQuery]
    public async Task<IActionResult> GetScenarios(ODataQueryOptions<ScenarioMetadata> queryOptions)
    {
        try
        {
            var scenarios = await _scenarioService.GetScenariosAsync();
            var validationSettings = new ODataValidationSettings
            {
                MaxTop = 100,
                AllowedQueryOptions = AllowedQueryOptions.Filter |
                                    AllowedQueryOptions.Select |
                                    AllowedQueryOptions.OrderBy |
                                    AllowedQueryOptions.Top |
                                    AllowedQueryOptions.Skip |
                                    AllowedQueryOptions.Count
            };

            queryOptions.Validate(validationSettings);

            var queryableScenarios = scenarios.AsQueryable();
            var results = queryOptions.ApplyTo(queryableScenarios);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            return BadRequest($"Error processing query: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateScenario([FromBody] CreateScenarioRequest request)
    {
        try
        {
            var scenario = await _scenarioService.CreateScenarioAsync(request);
            return CreatedAtAction(nameof(GetScenario), new { scenarioId = scenario.ScenarioId }, scenario);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid scenario creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scenario");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{scenarioId}")]
    public async Task<IActionResult> GetScenario(string scenarioId)
    {
        try
        {
            var scenario = await _scenarioService.GetScenarioAsync(scenarioId);
            if (scenario == null)
                return NotFound();
            return Ok(scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{scenarioId}")]
    public async Task<IActionResult> UpdateScenario(string scenarioId, [FromBody] CreateScenarioRequest request)
    {
        try
        {
            await _scenarioService.UpdateScenarioAsync(scenarioId, request);
            var updatedScenario = await _scenarioService.GetScenarioAsync(scenarioId);
            return Ok(updatedScenario);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid scenario update request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{scenarioId}")]
    public async Task<IActionResult> DeleteScenario(string scenarioId)
    {
        try
        {
            var scenario = await _scenarioService.GetScenarioAsync(scenarioId);
            if (scenario == null)
                return NotFound();

            await _scenarioService.DeleteScenarioAsync(scenarioId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{scenarioId}/train")]
    public async Task<IActionResult> Train(string scenarioId)
    {
        try
        {
            var scenario = await _scenarioService.GetScenarioAsync(scenarioId);
            if (scenario == null)
                return NotFound("Scenario not found");

            var r = await _scenarioService.CreateTrainJobAsync(scenarioId);
            return Ok(new { r.jobId, r.status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating train job for scenario {ScenarioId}", scenarioId);
            return BadRequest(ex.Message);
        }
    }
}