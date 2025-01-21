using System.ComponentModel.DataAnnotations;

namespace MLoop.Api.Models.Scenarios;

public class CreateScenarioRequest
{
    [Required]
    [MinLength(3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(classification|regression|recommendation|image-classification|text-classification|forecasting|object-detection)$")]
    public string MLType { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(10)]
    public List<string> Tags { get; set; } = [];
}

public class UpdateScenarioRequest
{
    public string? Name { get; set; }
    public string? MLType { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public string? BestModelId { get; set; }
}