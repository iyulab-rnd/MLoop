namespace MLoop.Api.Models;

public class CreateScenarioRequest
{
    public required string Name { get; set; }
    public required string Command { get; set; }  // classification, regression, recommendation 등
    public List<string> Tags { get; set; } = [];
}
