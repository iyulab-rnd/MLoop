namespace MLoop.Models.Messages;

public class ScalingMessage
{
    public string ScenarioId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}