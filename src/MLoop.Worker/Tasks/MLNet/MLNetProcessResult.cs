using MLoop.Models;

namespace MLoop.Worker.Tasks.MLNet;

public enum MLNetErrorType
{
    None,
    TimeoutException,
    OutOfMemoryException,
    InvalidOperationException,
    ArgumentException,
    DataProcessingError,
    TrainingError,
    ValidationError,
    UnknownError
}

public abstract class MLNetProcessResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public TimeSpan ProcessingTime { get; set; }
    public MLModelMetrics? Metrics { get; set; }
    public string? ErrorMessage { get; set; }
    public MLNetErrorType ErrorType { get; set; }

    public virtual async Task<bool> ParseMetricsAsync(string modelPath)
    {
        try
        {
            Metrics = await MLModelMetrics.ParseForMLNetAsync(modelPath);
            return Metrics != null;
        }
        catch
        {
            return false;
        }
    }
}