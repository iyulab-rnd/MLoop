using MLoop.Models.Workflows;

namespace MLoop.Worker.Pipeline;

public interface IPipeline
{
    Task ExecuteAsync(
        Workflow workflow,
        JobContext context,
        CancellationToken cancellationToken = default);
}