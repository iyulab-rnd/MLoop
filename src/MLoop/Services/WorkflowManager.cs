using Microsoft.Extensions.Logging;
using MLoop.Base;
using MLoop.Helpers;
using MLoop.Models.Workflows;

namespace MLoop.Services;

public class WorkflowManager : ScenarioManagerBase<Workflow>
{
    private const string WorkflowsDirectory = "workflows";

    public WorkflowManager(IFileStorage storage, ILogger<WorkflowManager> logger)
        : base(storage, logger)
    {
    }

    public override async Task<Workflow?> LoadAsync(string scenarioId, string workflowName)
    {
        var workflowPath = GetWorkflowPath(scenarioId, workflowName);

        if (!File.Exists(workflowPath))
        {
            return null;
        }

        var yaml = await File.ReadAllTextAsync(workflowPath);
        var workflow = YamlHelper.Deserialize<Workflow>(yaml)
            ?? throw new InvalidOperationException("Failed to deserialize workflow");

        workflow.ScenarioId = scenarioId;

        workflow.OriginalContent = yaml;
        return workflow;
    }

    public override async Task SaveAsync(string scenarioId, string workflowName, Workflow workflow)
    {
        var workflowPath = GetWorkflowPath(scenarioId, workflowName);
        var directory = Path.GetDirectoryName(workflowPath)!;

        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(workflowPath, YamlHelper.Serialize(workflow));
    }

    public override Task DeleteAsync(string scenarioId, string workflowName)
    {
        var workflowPath = GetWorkflowPath(scenarioId, workflowName);

        if (File.Exists(workflowPath))
        {
            File.Delete(workflowPath);
        }

        return Task.CompletedTask;
    }

    public override Task<bool> ExistsAsync(string scenarioId, string workflowName)
    {
        var workflowPath = GetWorkflowPath(scenarioId, workflowName);
        return Task.FromResult(File.Exists(workflowPath));
    }

    public string GetWorkflowsDirectory(string scenarioId)
    {
        return Path.Combine(_storage.GetScenarioBaseDir(scenarioId), WorkflowsDirectory);
    }

    public string GetWorkflowPath(string scenarioId, string workflowName)
    {
        return Path.Combine(GetWorkflowsDirectory(scenarioId), $"{workflowName}.yaml");
    }

    public async Task<IEnumerable<string>> GetWorkflowNamesAsync(string scenarioId)
    {
        var workflowsDir = GetWorkflowsDirectory(scenarioId);
        if (!Directory.Exists(workflowsDir))
        {
            return [];
        }

        var files = Directory.GetFiles(workflowsDir, "*.yaml");
        return files.Select(f => Path.GetFileNameWithoutExtension(f));
    }
}