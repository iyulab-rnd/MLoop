namespace MLoop.Worker.Tasks.CmdTask;

public class CmdProcessResult
{
    public bool Success { get; init; }
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public TimeSpan ProcessingTime { get; init; }
}
