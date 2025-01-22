using Microsoft.Extensions.Logging;
using MLoop.Models.Jobs;
using MLoop.Models.Workflows;
using System.Diagnostics;

namespace MLoop.Worker.Pipeline;

public class PipelineExecutor : IPipeline
{
    private readonly ILogger<PipelineExecutor> _logger;
    private readonly IStepRegistry _stepRegistry;

    public PipelineExecutor(
        IStepRegistry stepRegistry,
        ILogger<PipelineExecutor> logger)
    {
        _stepRegistry = stepRegistry;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Workflow workflow,
        JobContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ScenarioId"] = context.ScenarioId,
            ["JobId"] = context.JobId,
            ["WorkflowSteps"] = workflow.Steps.Count,
            ["WorkflowType"] = workflow.Type.ToString()
        });

        var completedSteps = new HashSet<string>();
        var stepTimer = new Stopwatch();

        await context.LogAsync(
            $"Starting workflow execution at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n" +
            $"Total steps: {workflow.Steps.Count}\n" +
            $"Workflow type: {workflow.Type}\n" +
            $"Available runners: {string.Join(", ", _stepRegistry.GetSupportedTypes())}");

        foreach (var step in workflow.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ValidateStepAsync(step, completedSteps);
                ValidateRunner(step);

                var runner = _stepRegistry.GetRunner(step.Type);

                using var stepScope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["StepName"] = step.Name,
                    ["StepType"] = step.Type
                });

                await context.LogAsync(
                    $"\nStarting step '{step.Name}' of type '{step.Type}' " +
                    $"at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");

                stepTimer.Restart();
                await runner.RunAsync(step, context);
                stepTimer.Stop();

                completedSteps.Add(step.Name);

                await context.LogAsync(
                    $"Completed step '{step.Name}' in {stepTimer.ElapsedMilliseconds}ms");

                context.Variables[$"{step.Name}_completed_at"] = DateTime.UtcNow;
                context.Variables[$"{step.Name}_elapsed_ms"] = stepTimer.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                await context.LogAsync(
                    $"Error in step '{step.Name}' after {stepTimer.ElapsedMilliseconds}ms:\n{ex}",
                    LogLevel.Error);

                _logger.LogError(ex,
                    "Error executing step {StepName} of type {StepType}",
                    step.Name,
                    step.Type);

                if (ex is JobProcessException)
                    throw;
                else
                    throw new JobProcessException(JobFailureType.ProcessError, "Error executing step", ex);
            }
        }

        await context.LogAsync(
            $"\nWorkflow completed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n" +
            $"Total steps completed: {completedSteps.Count}/{workflow.Steps.Count}");
    }

    private async Task ValidateStepAsync(WorkflowStep step, HashSet<string> completedSteps)
    {
        if (string.IsNullOrWhiteSpace(step.Name))
        {
            throw new JobProcessException(
                JobFailureType.ConfigurationError,
                "Step name is required");
        }

        if (string.IsNullOrWhiteSpace(step.Type))
        {
            throw new JobProcessException(
                JobFailureType.ConfigurationError,
                $"Step type is required for step '{step.Name}'");
        }

        await Task.CompletedTask;
    }

    private void ValidateRunner(WorkflowStep step)
    {
        if (!_stepRegistry.HasRunner(step.Type))
        {
            throw new JobProcessException(
                JobFailureType.ConfigurationError,
                $"No runner found for step type: {step.Type}. " +
                $"Available runners: {string.Join(", ", _stepRegistry.GetSupportedTypes())}");
        }
    }
}