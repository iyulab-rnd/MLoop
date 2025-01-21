namespace MLoop.Models.Jobs;

public enum MLJobStatus
{
    Waiting,
    Running,
    Completed,
    Failed
}

public enum MLJobType
{
    Train,
    Predict
}

public class MLJob : IScenarioEntity
{
    public string JobId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public MLJobStatus Status { get; set; } = MLJobStatus.Waiting;
    public string? WorkerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public List<MLJobStatusHistory> StatusHistory { get; set; } = [];
    public JobFailureType FailureType { get; set; }
    public string? ModelId { get; set; }
    public MLJobType JobType { get; set; } = MLJobType.Train;
    public Dictionary<string, object> Variables { get; set; } = [];

    public void AddStatusHistory(MLJobStatus status, string? workerId = null, string? message = null)
    {
        StatusHistory.Add(new MLJobStatusHistory
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            WorkerId = workerId,
            Message = message
        });
    }

    public void MarkAsStarted(string workerId)
    {
        Status = MLJobStatus.Running;
        WorkerId = workerId;
        StartedAt = DateTime.UtcNow;
        AddStatusHistory(Status, workerId, "Job started");
    }

    public void MarkAsCompleted()
    {
        Status = MLJobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        WorkerId = null;
        AddStatusHistory(Status, null, "Job completed successfully");
    }

    public void MarkAsFailed(JobFailureType failureType, string message)
    {
        Status = MLJobStatus.Failed;
        FailedAt = DateTime.UtcNow;
        FailureType = failureType;
        ErrorMessage = message;
        WorkerId = null;
        AddStatusHistory(Status, null, message);
    }
}