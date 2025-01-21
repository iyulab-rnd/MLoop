using MLoop.Base;
using MLoop.Models.Workflows;
using MLoop.Services;

namespace MLoop.Api.Services.Handlers;

public class WorkflowHandler : HandlerBase<Workflow>
{
    private readonly WorkflowManager _workflowManager;
    private readonly ILogger<WorkflowHandler> _logger;

    public WorkflowHandler(
        WorkflowManager workflowManager,
        ILogger<WorkflowHandler> logger) : base(logger)
    {
        _workflowManager = workflowManager;
        _logger = logger;
    }

    public override async Task<Workflow> ProcessAsync(Workflow workflow)
    {
        await _workflowManager.SaveAsync(workflow.ScenarioId, workflow.Name, workflow);
        return workflow;
    }

    public async Task ValidateAndSaveAsync(string scenarioId, string workflowName, string yamlContent)
    {
        // 1. YAML 형식 검증
        Workflow? workflow;
        try
        {
            workflow = YamlHelper.Deserialize<Workflow>(yamlContent);
            if (workflow == null)
            {
                throw new WorkflowValidationException("Invalid YAML format");
            }
        }
        catch (Exception ex)
        {
            throw new WorkflowValidationException($"Failed to parse YAML: {ex.Message}");
        }

        // 2. 워크플로우 유효성 검증
        ValidateWorkflow(workflow);

        // 3. 원본 YAML 텍스트 그대로 저장
        var workflowPath = _workflowManager.GetWorkflowPath(scenarioId, workflowName);
        var directory = Path.GetDirectoryName(workflowPath)!;

        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(workflowPath, yamlContent);

        _logger.LogInformation(
            "Saved workflow '{WorkflowName}' for scenario {ScenarioId}",
            workflowName, scenarioId);
    }

    private void ValidateWorkflow(Workflow workflow)
    {
        if (string.IsNullOrEmpty(workflow.Name))
            throw new WorkflowValidationException("Workflow name is required");

        if (workflow.Steps == null || !workflow.Steps.Any())
            throw new WorkflowValidationException("Workflow must contain at least one step");

        // 각 단계 검증
        foreach (var step in workflow.Steps)
        {
            if (string.IsNullOrEmpty(step.Name))
                throw new WorkflowValidationException("Step name is required");

            if (string.IsNullOrEmpty(step.Type))
                throw new WorkflowValidationException($"Step type is required for step '{step.Name}'");
        }
    }

    public override Task ValidateAsync(Workflow workflow)
    {
        if (string.IsNullOrEmpty(workflow.Name))
            throw new ValidationException("Workflow name is required");

        if (string.IsNullOrEmpty(workflow.ScenarioId))
            throw new ValidationException("ScenarioId is required");

        if (workflow.Steps == null || !workflow.Steps.Any())
            throw new ValidationException("Workflow must contain at least one step");

        // 각 단계 검증
        foreach (var step in workflow.Steps)
        {
            if (string.IsNullOrEmpty(step.Name))
                throw new ValidationException("Step name is required");

            if (string.IsNullOrEmpty(step.Type))
                throw new ValidationException($"Step type is required for step '{step.Name}'");
        }

        return Task.CompletedTask;
    }
}