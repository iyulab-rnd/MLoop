
using System.Text.Json.Serialization;

namespace MLoop.Models;

public class MLScenario
{
    public MLScenarioTypes Type { get; set; }
    public string Name { get; set; } = null!;
    public string? DataPath { get; set; }

    [JsonPropertyName("train-options")]
    public TrainOptions? TrainOptions { get; set; }
}
