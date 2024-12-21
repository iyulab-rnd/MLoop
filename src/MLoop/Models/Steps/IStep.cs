namespace MLoop.Models.Steps;

public interface IStep
{
    string Type { get; }
    string Name { get; }
    Dictionary<string, object>? Configuration { get; }
    IEnumerable<string>? Dependencies { get; }
}
