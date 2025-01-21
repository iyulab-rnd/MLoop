using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Services;
using MLoop.Models.Workflows;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly WorkflowService _workflowService;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        WorkflowService workflowService,
        ILogger<WorkflowsController> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkflows(string scenarioId)
    {
        try
        {
            var workflows = await _workflowService.GetWorkflowsAsync(scenarioId);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { error = "Error retrieving workflows" });
        }
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetWorkflow(string scenarioId, string name)
    {
        try
        {
            var yaml = await _workflowService.GetWorkflowYamlAsync(scenarioId, name);
            if (yaml == null)
                return NotFound(new { error = $"Workflow '{name}' not found in scenario {scenarioId}" });

            return Content(yaml, "text/yaml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow '{Name}' for scenario {ScenarioId}", name, scenarioId);
            return StatusCode(500, new { error = "Error retrieving workflow" });
        }
    }

    [HttpPost("{name?}")]
    [HttpPut("{name}")]
    public async Task<IActionResult> UpsertWorkflow(string scenarioId, string? name = "default_train")
    {
        if (!Request.ContentType?.Contains("yaml") ?? true)
        {
            return BadRequest(new { error = "Content-Type must be text/yaml" });
        }

        try
        {
            string yamlContent;
            using (var reader = new StreamReader(Request.Body))
            {
                yamlContent = await reader.ReadToEndAsync();
            }

            await _workflowService.ValidateAndSaveWorkflowAsync(scenarioId, name, yamlContent);
            return Ok(new { message = "Workflow saved successfully" });
        }
        catch (WorkflowValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving workflow '{Name}' for scenario {ScenarioId}", name, scenarioId);
            return StatusCode(500, new { error = "Error saving workflow" });
        }
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteWorkflow(string scenarioId, string name)
    {
        try
        {
            var workflow = await _workflowService.GetAsync(scenarioId, name);
            if (workflow == null)
                return NotFound(new { error = $"Workflow '{name}' not found in scenario {scenarioId}" });

            await _workflowService.DeleteAsync(scenarioId, name);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow '{Name}' for scenario {ScenarioId}", name, scenarioId);
            return StatusCode(500, new { error = "Error deleting workflow" });
        }
    }
}