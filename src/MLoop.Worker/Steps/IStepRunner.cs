using MLoop.Models.Steps;
using MLoop.Worker.Pipeline;

namespace MLoop.Worker.Steps;

public interface IStepRunner
{
    string Type { get; }

    Task RunAsync(IStep step, WorkflowContext context);
}