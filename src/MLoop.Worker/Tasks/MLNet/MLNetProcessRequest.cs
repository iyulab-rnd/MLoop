using MLoop.Worker.Pipeline;

namespace MLoop.Worker.Tasks.MLNet;

public abstract record MLNetProcessRequest
{
    public required string Command { get; init; }
    public required Dictionary<string, object> Arguments { get; init; }
    public required string BasePath { get; init; }
    public TimeSpan? Timeout { get; init; }
    public required JobContext Context { get; init; }
}
