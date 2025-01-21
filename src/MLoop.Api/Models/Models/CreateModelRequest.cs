namespace MLoop.Api.Models.Models;

public class CreateModelRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string MLType { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, object>? Arguments { get; set; }
}

public class UpdateModelRequest
{
    public string? Command { get; set; }
    public Dictionary<string, object>? Arguments { get; set; }
}