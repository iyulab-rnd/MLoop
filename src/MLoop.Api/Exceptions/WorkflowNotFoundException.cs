namespace MLoop.Api.Exceptions;

public class WorkflowNotFoundException : Exception
{
    public string WorkflowName { get; }
    public string ScenarioId { get; }

    public WorkflowNotFoundException(string workflowName, string scenarioId)
        : base($"Workflow '{workflowName}' not found in scenario '{scenarioId}'")
    {
        WorkflowName = workflowName;
        ScenarioId = scenarioId;
    }
}
