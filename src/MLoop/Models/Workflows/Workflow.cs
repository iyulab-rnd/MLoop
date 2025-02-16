using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace MLoop.Models.Workflows;

public class Workflow : IScenarioEntity
{
    private static readonly Regex NamePattern = new("^[a-zA-Z][a-zA-Z0-9_]*$");

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public JobTypes Type { get; set; }

    [YamlMember(Alias = "env")]
    public Dictionary<string, object> Environment { get; set; } = [];

    [YamlMember(Alias = "dataset")]
    public string? DatasetName { get; set; }  // DatasetId -> DatasetName 변경

    public List<WorkflowStep> Steps { get; set; } = [];

    [YamlIgnore]
    public string? OriginalContent { get; set; }

    [YamlIgnore]
    public string ScenarioId { get; set; } = null!;

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || !NamePattern.IsMatch(Name))
        {
            return false;
        }
        if (Steps.Count == 0)
        {
            return false;
        }
        return Steps.All(step => step.IsValid);
    }
}