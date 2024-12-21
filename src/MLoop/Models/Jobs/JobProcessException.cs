namespace MLoop.Models.Jobs;

public class JobProcessException : Exception
{
    public JobFailureType FailureType { get; }

    public JobProcessException(JobFailureType failureType, string message)
        : base(message)
    {
        FailureType = failureType;
    }

    public JobProcessException(JobFailureType failureType, string message, Exception innerException)
        : base(message, innerException)
    {
        FailureType = failureType;
    }
}
