using MLoop.Models.Workflows;

namespace MLoop.Worker.Pipeline;

public interface IStepRunner
{
    string Type { get; }
    Task RunAsync(WorkflowStep step, JobContext context);
}