namespace MLoop.Models.Workflows;

public class WorkflowStep
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public Dictionary<string, object>? Config { get; set; }

    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Type);
}
