namespace MLoop.Models.Jobs;

public class MLJobResult
{
    public bool Success { get; set; }
    public DateTime CompletedAt { get; set; }
    public JobFailureType? FailureType { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = [];
}
