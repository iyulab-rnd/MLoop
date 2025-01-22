using MLoop.Worker.Pipeline;

namespace MLoop.Worker.Tasks;

public record TaskRequest
{
    /// <summary>
    /// Base directory path for execution
    /// </summary>
    public required string BasePath { get; init; }

    /// <summary>
    /// Execution timeout. If null, default timeout will be used
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    public required JobContext Context { get; init; }  // 컨텍스트 추가

}
