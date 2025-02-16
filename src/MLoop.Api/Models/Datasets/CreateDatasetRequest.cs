namespace MLoop.Api.Models.Datasets;

public class CreateDatasetRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
}

public class UpdateDatasetRequest
{
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
}