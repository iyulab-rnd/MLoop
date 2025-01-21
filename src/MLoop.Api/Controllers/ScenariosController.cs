using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using MLoop.Api.Models.Scenarios;
using MLoop.Models;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/[controller]")]
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
    public async Task<IActionResult> GetScenarios(ODataQueryOptions<MLScenario> queryOptions)
    {
        try
        {
            var scenarios = await _scenarioService.GetAllScenariosAsync();

            // OData 옵션 검증
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
            _logger.LogError(ex, "Error retrieving scenarios");
            return StatusCode(500, new { error = "Error retrieving scenarios" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateScenario([FromBody] CreateScenarioRequest request)
    {
        try
        {
            var scenario = await _scenarioService.CreateAsync(request);
            return CreatedAtAction(
                nameof(GetScenario),
                new { scenarioId = scenario.ScenarioId },
                scenario);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scenario");
            return StatusCode(500, new { error = "Error creating scenario" });
        }
    }

    [HttpGet("{scenarioId}")]
    public async Task<IActionResult> GetScenario(string scenarioId)
    {
        try
        {
            var scenario = await _scenarioService.GetAsync(scenarioId);
            if (scenario == null)
                return NotFound(new { error = $"Scenario {scenarioId} not found" });
            return Ok(scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error retrieving scenario" });
        }
    }

    [HttpPut("{scenarioId}")]
    public async Task<IActionResult> UpdateScenario(string scenarioId, [FromBody] UpdateScenarioRequest request)
    {
        try
        {
            var scenario = await _scenarioService.UpdateAsync(scenarioId, request);
            return Ok(scenario);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Scenario {scenarioId} not found" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error updating scenario" });
        }
    }

    [HttpDelete("{scenarioId}")]
    public async Task<IActionResult> DeleteScenario(string scenarioId)
    {
        try
        {
            var scenario = await _scenarioService.GetAsync(scenarioId);
            if (scenario == null)
                return NotFound(new { error = $"Scenario {scenarioId} not found" });

            await _scenarioService.DeleteAsync(scenarioId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error deleting scenario" });
        }
    }

    [HttpPost("{scenarioId}/train")]
    [HttpPost("{scenarioId}/train/{workflowName}")]  // 두 라우트 지원
    public async Task<IActionResult> Train(string scenarioId, string? workflowName = "default_train")
    {
        try
        {
            var (jobId, status) = await _scenarioService.CreateTrainJobAsync(scenarioId, workflowName);
            return Ok(new
            {
                jobId,
                status = status.ToString(),
                workflowName
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Scenario {scenarioId} not found" });
        }
        catch (WorkflowNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating train job for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error creating training job" });
        }
    }

    [HttpGet("{scenarioId}/train/validate")]
    public async Task<IActionResult> ValidateTrainingPrerequisites(string scenarioId)
    {
        try
        {
            var scenario = await _scenarioService.GetAsync(scenarioId);
            if (scenario == null)
                return NotFound(new { error = $"Scenario {scenarioId} not found" });

            var (isValid, issues) = await _scenarioService.ValidateTrainingPrerequisitesAsync(scenarioId);
            return Ok(new
            {
                isValid,
                issues
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating training prerequisites for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error validating training prerequisites" });
        }
    }

    [HttpGet("{scenarioId}/status")]
    public async Task<IActionResult> GetScenarioStatus(string scenarioId)
    {
        try
        {
            var status = await _scenarioService.GetScenarioStatusAsync(scenarioId);
            if (status == null)
                return NotFound(new { error = $"Scenario {scenarioId} not found" });

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error retrieving scenario status" });
        }
    }
}