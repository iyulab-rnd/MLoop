using System.ComponentModel.DataAnnotations;

namespace MLoop.Models;

public class MLModel : IScenarioEntity
{
    public string ScenarioId { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    [Required]
    public string MLType { get; set; } = string.Empty;

    [Required]
    public string Command { get; set; } = string.Empty;

    [Required]
    public Dictionary<string, object> Arguments { get; set; } = [];

    public MLModelMetrics? Metrics { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MLModel(
        string modelId,
        string mlType,
        string command,
        Dictionary<string, object> arguments)
    {
        ModelId = modelId;
        Command = command;
        Arguments = arguments;
        MLType = mlType;
    }
}
