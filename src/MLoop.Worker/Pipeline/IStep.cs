namespace MLoop.Worker.Pipeline;

public interface IStep
{
    string Name { get; }
    string Type { get; }
    Dictionary<string, object>? Configuration { get; }
    IEnumerable<string>? Dependencies { get; }
}
