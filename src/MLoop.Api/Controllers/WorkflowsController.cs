using Microsoft.AspNetCore.Mvc;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MLoop.Storages;
using MLoop.Models.Workflows;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("scenarios/{scenarioId}/workflows")]
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

    [HttpPost("train")]
    public async Task<IActionResult> UpdateTrainWorkflow(string scenarioId, [FromBody] string yamlContent)
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

            // Create workflows directory
            var scenarioBaseDir = _storage.GetScenarioBaseDir(scenarioId);
            var workflowsDir = Path.Combine(scenarioBaseDir, "workflows");
            Directory.CreateDirectory(workflowsDir);

            // Save original content directly
            var trainWorkflowPath = Path.Combine(workflowsDir, "train.yaml");
            await System.IO.File.WriteAllTextAsync(trainWorkflowPath, yamlContent);

            _logger.LogInformation("Updated train workflow for scenario {ScenarioId}", scenarioId);
            return Ok(new { message = "Train workflow updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating train workflow for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Error updating workflow");
        }
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