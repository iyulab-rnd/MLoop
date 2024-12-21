using Microsoft.Extensions.Logging;
using MLoop.Models;
using MLoop.Models.Jobs;
using MLoop.Models.Steps;
using MLoop.Worker.Pipeline;
using MLoop.Worker.Steps;
using MLoop.Worker.Tasks.CmdTask;

namespace MLoop.Worker.Tasks.MLNetTrainTask;

public class MLNetTrainRunner : IStepRunner
{
    private readonly MLNetTrainProcessor _processor;
    private readonly MLNetTrainOptionsAdjuster _optionsAdjuster;
    private readonly ILogger<MLNetTrainRunner> _logger;

    public string Type => "mlnet-train";

    public MLNetTrainRunner(
        MLNetTrainProcessor processor,
        ILogger<MLNetTrainRunner> logger)
    {
        _processor = processor;
        _optionsAdjuster = new MLNetTrainOptionsAdjuster(logger);
        _logger = logger;
    }

    public async Task RunAsync(IStep step, WorkflowContext context)
    {
        await ValidateConfigurationAsync(step, context);
        var request = BuildRequest(step, context);
        var result = await ExecuteProcessAsync(request, step, context);

        if (result.Success)
        {
            await SaveModelMetadataAsync(step, context, request, result);
        }
        else
        {
            throw new JobProcessException(
                JobFailureType.MLNetError,
                $"MLNet training failed: {result.StandardError}");
        }
    }

    private async Task SaveModelMetadataAsync(
        IStep step,
        WorkflowContext context,
        MLNetTrainRequest request,
        MLNetTrainProcessResult result)
    {
        var modelId = context.Variables["ModelId"]?.ToString()
            ?? throw new InvalidOperationException("ModelId not found in context");

        // 1. Save model metadata
        var model = new MLModel(
            modelId: modelId,
            mlType: request.Command,
            command: request.Command,
            arguments: request.Arguments
        );

        if (result.Metrics != null)
        {
            model.Metrics = result.Metrics;
        }

        var modelMetadataPath = context.GetModelMetadataPath(modelId);
        var modelMetadataJson = JsonHelper.Serialize(model);
        await File.WriteAllTextAsync(modelMetadataPath, modelMetadataJson);

        // 2. Save metrics separately if available
        if (result.Metrics != null)
        {
            var metricsPath = context.GetModelMetricsPath(modelId);
            var metricsJson = JsonHelper.Serialize(result.Metrics);
            await File.WriteAllTextAsync(metricsPath, metricsJson);
        }

        _logger.LogInformation(
            "Saved model metadata and metrics for ModelId: {ModelId}, MLType: {MLType}",
            modelId,
            request.Command);
    }

    private MLNetTrainRequest BuildRequest(IStep step, WorkflowContext context)
    {
        _logger.LogDebug("Step configuration: {Config}",
            JsonHelper.Serialize(step.Configuration));

        if (step.Configuration?["command"] is not string command)
        {
            throw new ArgumentException("Command must be a string value");
        }

        Dictionary<string, object> args;
        try
        {
            if (step.Configuration["args"] is Dictionary<string, object> directDict)
            {
                args = directDict;
            }
            else if (step.Configuration["args"] is IDictionary<object, object> objDict)
            {
                args = objDict.ToDictionary(
                    k => k.Key.ToString()!,
                    v => v.Value,
                    StringComparer.OrdinalIgnoreCase
                );
            }
            else
            {
                _logger.LogError("Unexpected args type: {ArgsType}",
                    step.Configuration["args"]?.GetType().FullName ?? "null");
                throw new ArgumentException($"Unexpected args type: {step.Configuration["args"]?.GetType().FullName ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting args to Dictionary<string, object>");
            throw new ArgumentException("Invalid args format in configuration", ex);
        }

        // Convert relative paths to absolute paths
        if (args.TryGetValue("dataset", out var datasetPath))
        {
            args["dataset"] = Path.Combine(
                context.Storage.GetScenarioDataDir(context.ScenarioId),
                datasetPath.ToString()!);
        }

        if (args.TryGetValue("validation-dataset", out var validationPath))
        {
            args["validation-dataset"] = Path.Combine(
                context.Storage.GetScenarioDataDir(context.ScenarioId),
                validationPath.ToString()!);
        }

        var modelId = context.Variables["ModelId"]?.ToString()
            ?? throw new InvalidOperationException("ModelId not found in context");

        // Adjust and validate options
        var config = new CmdConfig(command, args);
        var adjustedConfig = _optionsAdjuster.AdjustOptions(config);

        return new MLNetTrainRequest
        {
            BasePath = context.GetModelPath(modelId),
            Command = adjustedConfig.Command,
            Arguments = adjustedConfig.Args,
            Timeout = TimeSpan.FromHours(8),
            Context = context
        };
    }

    private Task ValidateConfigurationAsync(IStep step, WorkflowContext context)
    {
        var config = step.Configuration;
        if (config == null)
        {
            throw new ArgumentException("Configuration is required for MLNet training step");
        }

        if (!config.TryGetValue("command", out var commandObj))
        {
            throw new ArgumentException("Command is required in step configuration");
        }

        if (!config.TryGetValue("args", out var argsObj))
        {
            throw new ArgumentException("Arguments are required in step configuration");
        }

        return Task.CompletedTask;
    }

    private async Task<MLNetTrainProcessResult> ExecuteProcessAsync(
        MLNetTrainRequest request,
        IStep step,
        WorkflowContext context)
    {
        return await _processor.TrainAsync(request, context.CancellationToken);
    }
}