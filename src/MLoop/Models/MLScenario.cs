using System.ComponentModel.DataAnnotations;

namespace MLoop.Models;

public class MLScenario
{
    [Key]
    public string ScenarioId { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [RegularExpression("^(classification|regression|recommendation|image-classification|text-classification|forecasting|object-detection)$")]
    public string MLType { get; set; } = string.Empty;

    [MaxLength(10)]
    public List<string> Tags { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public string? BestModelId { get; set; }

    public MLScenario()
    {
        CreatedAt = DateTime.UtcNow;
    }
}