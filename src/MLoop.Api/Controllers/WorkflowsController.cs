using Microsoft.AspNetCore.Mvc;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MLoop.Storages;
using MLoop.Models.Workflows;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/workflows")]
public class WorkflowController : ControllerBase
{
    private readonly IFileStorage _storage;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IFileStorage storage,
        ILogger<WorkflowController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpGet("train")]
    public async Task<IActionResult> GetTrainWorkflow(string scenarioId)
    {
        return await GetWorkflow(scenarioId, "train");
    }

    [HttpGet("predict")]
    public async Task<IActionResult> GetPredictWorkflow(string scenarioId)
    {
        return await GetWorkflow(scenarioId, "predict");
    }

    [HttpPost("train")]
    public async Task<IActionResult> UpdateTrainWorkflow(string scenarioId, [FromBody] string yamlContent)
    {
        return await UpdateWorkflow(scenarioId, yamlContent, "train");
    }

    [HttpPost("predict")]
    public async Task<IActionResult> UpdatePredictWorkflow(string scenarioId, [FromBody] string yamlContent)
    {
        return await UpdateWorkflow(scenarioId, yamlContent, "predict");
    }

    private async Task<IActionResult> GetWorkflow(string scenarioId, string workflowType)
    {
        try
        {
            var workflowPath = GetWorkflowPath(scenarioId, workflowType);

            if (!System.IO.File.Exists(workflowPath))
            {
                return NoContent();
            }

            var yamlContent = await System.IO.File.ReadAllTextAsync(workflowPath);

            // Validate the existing YAML to ensure it's still valid
            if (!ValidateWorkflow(yamlContent, out var error))
            {
                _logger.LogWarning("Invalid {WorkflowType} workflow found for scenario {ScenarioId}: {Error}",
                    workflowType, scenarioId, error);
                return StatusCode(500, $"Stored workflow is invalid: {error}");
            }

            return Ok(yamlContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {WorkflowType} workflow for scenario {ScenarioId}",
                workflowType, scenarioId);
            return StatusCode(500, $"Error retrieving {workflowType} workflow");
        }
    }

    private async Task<IActionResult> UpdateWorkflow(string scenarioId, string yamlContent, string workflowType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                return BadRequest("Workflow content is required");
            }

            // Validate YAML structure
            if (!ValidateWorkflow(yamlContent, out var error))
            {
                return BadRequest(error);
            }

            var workflowPath = GetWorkflowPath(scenarioId, workflowType);
            EnsureWorkflowDirectoryExists(scenarioId);

            // Save content
            await System.IO.File.WriteAllTextAsync(workflowPath, yamlContent);

            _logger.LogInformation("Updated {WorkflowType} workflow for scenario {ScenarioId}",
                workflowType, scenarioId);
            return Ok(new { message = $"{workflowType} workflow updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {WorkflowType} workflow for scenario {ScenarioId}",
                workflowType, scenarioId);
            return StatusCode(500, $"Error updating {workflowType} workflow");
        }
    }

    private string GetWorkflowPath(string scenarioId, string workflowType)
    {
        var scenarioBaseDir = _storage.GetScenarioBaseDir(scenarioId);
        var workflowsDir = Path.Combine(scenarioBaseDir, "workflows");
        return Path.Combine(workflowsDir, $"{workflowType}.yaml");
    }

    private void EnsureWorkflowDirectoryExists(string scenarioId)
    {
        var scenarioBaseDir = _storage.GetScenarioBaseDir(scenarioId);
        var workflowsDir = Path.Combine(scenarioBaseDir, "workflows");
        Directory.CreateDirectory(workflowsDir);
    }

    private static bool ValidateWorkflow(string yamlContent, out string? error)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var workflow = deserializer.Deserialize<WorkflowConfig>(yamlContent);

            if (workflow?.Steps == null || !workflow.Steps.Any())
            {
                error = "Workflow must contain at least one step";
                return false;
            }

            foreach (var step in workflow.Steps)
            {
                if (string.IsNullOrEmpty(step.Name))
                {
                    error = "Each step must have a name";
                    return false;
                }
                if (string.IsNullOrEmpty(step.Type))
                {
                    error = "Each step must have a type";
                    return false;
                }
            }

            error = null;
            return true;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            error = $"Invalid YAML format: {ex.Message}";
            return false;
        }
    }
}