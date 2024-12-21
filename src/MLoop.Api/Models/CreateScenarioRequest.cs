using System.ComponentModel.DataAnnotations;

namespace MLoop.Api.Models;

public class CreateScenarioRequest
{
    [Required, MinLength(3)]
    public string Name { get; set; } = null!;

    [Required]
    [RegularExpression("^(classification|regression|recommendation|image-classification|text-classification|forecasting|object-detection)$")]
    public string MLType { get; set; } = null!;

    [MaxLength(10)]
    public List<string> Tags { get; set; } = [];
}