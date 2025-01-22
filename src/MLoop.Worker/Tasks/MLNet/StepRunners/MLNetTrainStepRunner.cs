using Microsoft.Extensions.Logging;
using MLoop.Models;
using MLoop.Models.Workflows;
using MLoop.Services;
using MLoop.Worker.Pipeline;
using MLoop.Worker.Tasks.MLNet.Train;

namespace MLoop.Worker.Tasks.MLNet.StepRunners;

public class MLNetTrainStepRunner : IStepRunner
{
    private readonly MLNetTrainProcessor _processor;
    private readonly BestModelManager _bestModelManager;
    private readonly ILogger<MLNetTrainStepRunner> _logger;

    public string Type => "mlnet-train";

    public MLNetTrainStepRunner(
        MLNetTrainProcessor processor,
        BestModelManager bestModelManager,
        ILogger<MLNetTrainStepRunner> logger)
    {
        _processor = processor;
        _bestModelManager = bestModelManager;
        _logger = logger;
    }

    public async Task RunAsync(WorkflowStep step, JobContext context)
    {
        await ValidateConfigurationAsync(step, context);
        var request = BuildRequest(step, context);
        var result = await ExecuteProcessAsync(request, step, context);

        if (result.Success)
        {
            await SaveModelMetadataAsync(step, context, request, result);

            // Training 성공하고 메트릭이 있는 경우에만 BestModel 업데이트 시도
            if (result.Metrics != null)
            {
                await _bestModelManager.UpdateBestModelIfBetterAsync(
                    request.Context.ScenarioId,
                    request.Context.ModelId!);
            }
        }
    }

    private MLNetTrainRequest BuildRequest(WorkflowStep step, JobContext context)
    {
        if (step.Config == null) throw new ArgumentException("Configuration is required for MLNet training step");
        _logger.LogDebug("Step configuration: {Config}", JsonHelper.Serialize(step.Config));

        var command = step.Config.GetValueOrDefault<string>("command")
            ?? throw new ArgumentException("Command must be a string value");

        var args = ArgumentHelper.ParseConfigArguments(_logger,
            step.Config.GetValueOrDefault<IDictionary<string, object>>("args"));

        var modelId = context.ModelId
            ?? throw new InvalidOperationException("ModelId not found in context");

        return new MLNetTrainRequest
        {
            BasePath = context.GetModelPath(modelId),
            Command = command,
            Arguments = args.ToDictionary(),
            Timeout = TimeSpan.FromHours(8),
            Context = context
        };
    }

    private Task ValidateConfigurationAsync(WorkflowStep step, JobContext context)
    {
        var config = step.Config;
        if (config == null)
        {
            throw new ArgumentException("Configuration is required for MLNet training step");
        }

        if (!config.ContainsKey("command"))
        {
            throw new ArgumentException("Command is required in step configuration");
        }

        if (!config.ContainsKey("args"))
        {
            throw new ArgumentException("Arguments are required in step configuration");
        }

        return Task.CompletedTask;
    }

    private async Task<MLNetTrainResult> ExecuteProcessAsync(
        MLNetTrainRequest request,
        WorkflowStep step,
        JobContext context)
    {
        return await _processor.TrainAsync(request, context.CancellationToken);
    }

    private async Task SaveModelMetadataAsync(
        WorkflowStep step,
        JobContext context,
        MLNetTrainRequest request,
        MLNetTrainResult result)
    {
        var modelId = context.ModelId ?? throw new InvalidOperationException("ModelId not found in context");

        // Save model metadata
        var model = new MLModel(
            modelId: modelId,
            mlType: request.Command,
            command: request.Command,
            arguments: request.Arguments
        )
        {
            Metrics = result.Metrics
        };

        var modelPath = context.GetModelPath(modelId);
        var modelMetadataPath = Path.Combine(modelPath, "model.json");
        var modelMetadataJson = JsonHelper.Serialize(model);
        await File.WriteAllTextAsync(modelMetadataPath, modelMetadataJson);

        _logger.LogInformation(
            "Saved model metadata and metrics for ModelId: {ModelId}, MLType: {MLType}",
            modelId,
            request.Command);
    }
}