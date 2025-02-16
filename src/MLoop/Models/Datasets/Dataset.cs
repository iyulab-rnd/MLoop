using System.Text.RegularExpressions;

namespace MLoop.Models.Datasets;

public class Dataset
{
    // Name을 식별자로 사용 (영문, 숫자, 하이픈, 언더스코어만 허용)
    private static readonly Regex NamePattern = new("^[a-zA-Z][a-zA-Z0-9_-]*$");

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? Size { get; set; }

    public bool ValidateName() => !string.IsNullOrWhiteSpace(Name) && NamePattern.IsMatch(Name);
}