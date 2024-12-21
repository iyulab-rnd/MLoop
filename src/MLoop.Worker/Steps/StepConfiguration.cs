using System.ComponentModel.DataAnnotations;

namespace MLoop.Worker.Steps;

public class StepConfiguration
{
    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, object>? Parameters { get; set; }

    public IEnumerable<string>? Dependencies { get; set; }

    public IEnumerable<string> GetDependencies() => Dependencies ?? Enumerable.Empty<string>();
}
