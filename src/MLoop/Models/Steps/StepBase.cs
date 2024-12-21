namespace MLoop.Models.Steps;

public abstract class StepBase : IStep
{
    public string Type { get; }
    public string Name { get; }
    public Dictionary<string, object>? Configuration { get; set; }
    public IEnumerable<string>? Dependencies { get; set; }

    protected StepBase(string type, string name)
    {
        Type = type;
        Name = name;
    }
}
