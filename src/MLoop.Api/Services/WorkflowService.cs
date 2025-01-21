using MLoop.Api.Models.Workflows;
using MLoop.Models.Workflows;
using MLoop.Services;

namespace MLoop.Api.Services;

public class WorkflowService
{
    private readonly WorkflowManager _workflowManager;
    private readonly WorkflowHandler _workflowHandler;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        WorkflowManager workflowManager,
        WorkflowHandler workflowHandler,
        ILogger<WorkflowService> logger)
    {
        _workflowManager = workflowManager;
        _workflowHandler = workflowHandler;
        _logger = logger;
    }

    public async Task<Workflow?> GetAsync(string scenarioId, string workflowName)
    {
        return await _workflowManager.LoadAsync(scenarioId, workflowName);
    }

    public async Task DeleteAsync(string scenarioId, string workflowName)
    {
        await _workflowManager.DeleteAsync(scenarioId, workflowName);
    }

    public async Task<string?> GetWorkflowYamlAsync(string scenarioId, string name)
    {
        var workflowPath = _workflowManager.GetWorkflowPath(scenarioId, name);
        if (!File.Exists(workflowPath))
            return null;

        return await File.ReadAllTextAsync(workflowPath);
    }

    public async Task<IEnumerable<WorkflowSummary>> GetWorkflowsAsync(string scenarioId)
    {
        var summaries = new List<WorkflowSummary>();
        var workflowNames = await _workflowManager.GetWorkflowNamesAsync(scenarioId);

        foreach (var name in workflowNames)
        {
            var workflow = await GetAsync(scenarioId, name);
            if (workflow != null)
            {
                summaries.Add(new WorkflowSummary
                {
                    Name = name,
                    Type = workflow.Type.ToString()
                });
            }
        }

        return summaries;
    }

    public async Task ValidateAndSaveWorkflowAsync(string scenarioId, string name, string yamlContent)
    {
        await _workflowHandler.ValidateAndSaveAsync(scenarioId, name, yamlContent);
    }
}