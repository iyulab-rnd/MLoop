namespace MLoop.Models.Workflows;

public class WorkflowConfig
{
    public List<WorkflowStepConfig> Steps { get; set; } = new();
}

public class WorkflowStepConfig
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string>? Needs { get; set; }
    public Dictionary<string, object>? Config { get; set; }

    public override string ToString()
    {
        return $"Step '{Name}' (Type: {Type}, Config: {JsonHelper.Serialize(Config)})";
    }
}