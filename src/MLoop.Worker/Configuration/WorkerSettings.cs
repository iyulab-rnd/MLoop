namespace MLoop.Worker.Configuration;

public class WorkerSettings
{
    public string? WorkerId { get; set; }
    public TimeSpan JobTimeout { get; set; } = TimeSpan.FromHours(2);
    public TimeSpan JobPollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
}