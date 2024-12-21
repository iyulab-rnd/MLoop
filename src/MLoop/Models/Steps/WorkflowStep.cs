namespace MLoop.Models.Steps;

public class WorkflowStep : StepBase
{
    public WorkflowStep(string type, string name) : base(type, name)
    {
    }

    public static WorkflowStep FromWorkflowConfig(Models.Workflows.WorkflowStepConfig config)
    {
        var step = new WorkflowStep(config.Type, config.Name);

        // Config가 null이 아닌 경우에만 할당
        if (config.Config != null)
        {
            // Dictionary 변환이 제대로 되었는지 확인을 위한 로깅 추가
            step.Configuration = new Dictionary<string, object>(config.Config, StringComparer.OrdinalIgnoreCase);
        }

        if (config.Needs != null)
        {
            step.Dependencies = config.Needs;
        }

        return step;
    }
}
