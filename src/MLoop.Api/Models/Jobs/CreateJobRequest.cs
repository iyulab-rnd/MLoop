using MLoop.Models.Jobs;

namespace MLoop.Api.Models.Jobs;

public class CreateJobRequest
{
    public string? JobId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public MLJobType JobType { get; set; } = MLJobType.Train;
    public string? ModelId { get; set; }
    public Dictionary<string, object>? Variables { get; set; }
}

public class UpdateJobRequest
{
    public MLJobStatus? Status { get; set; }
    public string? WorkerId { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Variables { get; set; }
}

public class JobCreatedResponse
{
    public string JobId { get; set; } = string.Empty;
    public MLJobStatus Status { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
}