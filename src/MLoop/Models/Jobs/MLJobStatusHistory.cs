namespace MLoop.Models.Jobs;

public class MLJobStatusHistory
{
    public MLJobStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string? WorkerId { get; set; }
    public string? Message { get; set; }
}