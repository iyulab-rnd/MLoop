namespace MLoop.Worker.Tasks.CmdTask;

// Base request record for all MLNet requests
public record CmdProcessRequest : TaskRequest
{
    /// <summary>
    /// MLNet training command (e.g., "classification", "regression", etc.)
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Training arguments for MLNet CLI
    /// </summary>
    public required Dictionary<string, object> Arguments { get; init; }
}
