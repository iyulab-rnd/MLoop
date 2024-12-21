using Microsoft.Extensions.Logging;
using MLoop.Models.Steps;
using MLoop.Models.Workflows;
using MLoop.Models.Jobs;
using MLoop.Worker.Steps.Registry;
using System.Diagnostics;

namespace MLoop.Worker.Pipeline;

public class PipelineExecutor
{
    private readonly StepRegistry _stepRegistry;
    private readonly ILogger<PipelineExecutor> _logger;

    public PipelineExecutor(
        StepRegistry stepRegistry,
        ILogger<PipelineExecutor> logger)
    {
        _stepRegistry = stepRegistry;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        WorkflowConfig workflow,
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ScenarioId"] = context.ScenarioId,
            ["JobId"] = context.JobId,
            ["WorkflowSteps"] = workflow.Steps.Count
        });

        var completedSteps = new HashSet<string>();
        var stepTimer = new Stopwatch();

        await LogToJobFile(context,
            $"Starting workflow execution at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n" +
            $"Total steps: {workflow.Steps.Count}\n");

        foreach (var workflowStep in workflow.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Transform workflow step to IStep
            var step = WorkflowStep.FromWorkflowConfig(workflowStep);

            // Check dependencies with try/catch for better error logging
            try
            {
                CheckStepDependencies(step, completedSteps);
            }
            catch (Exception ex)
            {
                var message = $"Error checking dependencies for step '{step.Name}': {ex.Message}";
                await LogToJobFile(context, message);
                _logger.LogError(ex, message);
                throw new JobProcessException(JobFailureType.ConfigurationError, message, ex);
            }

            // Get and validate runner
            if (!_stepRegistry.HasRunner(step.Type))
            {
                var message = $"No runner found for step type: {step.Type}";
                await LogToJobFile(context, $"Error: {message}");
                throw new JobProcessException(JobFailureType.ConfigurationError, message);
            }

            var runner = _stepRegistry.GetRunner(step.Type);

            using var stepScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["StepName"] = step.Name,
                ["StepType"] = step.Type
            });

            await LogToJobFile(context,
                $"\nStarting step '{step.Name}' of type '{step.Type}' " +
                $"at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");

            try
            {
                stepTimer.Restart();
                await runner.RunAsync(step, context);
                stepTimer.Stop();

                completedSteps.Add(step.Name);

                await LogToJobFile(context,
                    $"Completed step '{step.Name}' in {stepTimer.ElapsedMilliseconds}ms");

                // Store step execution results
                context.Variables[$"{step.Name}_completed_at"] = DateTime.UtcNow;
                context.Variables[$"{step.Name}_elapsed_ms"] = stepTimer.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                await LogToJobFile(context,
                    $"Error in step '{step.Name}' after {stepTimer.ElapsedMilliseconds}ms:\n{ex}");
                _logger.LogError(ex, "Error executing step {StepName} of type {StepType}", step.Name, step.Type);
                if (ex is JobProcessException)
                    throw;
                else
                    throw new JobProcessException(JobFailureType.ProcessError, "Error executing step", ex);
            }
        }

        await LogToJobFile(context,
            $"\nWorkflow completed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n" +
            $"Total steps completed: {completedSteps.Count}/{workflow.Steps.Count}");
    }

    private void CheckStepDependencies(IStep step, HashSet<string> completedSteps)
    {
        if (step.Dependencies != null)
        {
            var missingDeps = step.Dependencies
                .Where(dep => !completedSteps.Contains(dep))
                .ToList();

            if (missingDeps.Any())
            {
                var message = $"Step '{step.Name}' requires steps that haven't been completed: {string.Join(", ", missingDeps)}";
                throw new InvalidOperationException(message);
            }
        }
    }

    private async Task LogToJobFile(WorkflowContext context, string message)
    {
        try
        {
            var logPath = context.GetJobLogsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            await File.AppendAllTextAsync(logPath, $"{message}\n", context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to job log file: {Message}", message);
        }
    }
}
