using Microsoft.Extensions.Logging;
using MLoop.Models.Steps;
using MLoop.Worker.Pipeline;
using MLoop.Worker.Steps;
using MLoop.Worker.Tasks.MLNetTrainTask;

namespace MLoop.Worker.Tasks.MLNetPredictTask;

public class MLNetPredictRunner : IStepRunner
{
    private readonly MLNetPredictProcessor _processor;

    public string Type => "mlnet-predict";

    public MLNetPredictRunner(
        MLNetPredictProcessor processor,
        ILogger<MLNetPredictRunner> logger)
    {
        _processor = processor;
    }

    public async Task RunAsync(IStep step, WorkflowContext context)
    {
        await ValidateConfigurationAsync(step, context);
        var reqeust = BuildRequest(step, context);
        await ExecuteProcessAsync(reqeust, step, context);
    }

    private Task ValidateConfigurationAsync(IStep step, WorkflowContext context)
    {
        if (step.Configuration == null || !step.Configuration.TryGetValue("model-id", out _))
        {
            throw new ArgumentException("model-id is required for prediction step");
        }

        var modelId = step.Configuration["model-id"].ToString()!;
        var modelPath = context.GetModelPath(modelId);

        if (!Directory.Exists(modelPath))
        {
            throw new DirectoryNotFoundException($"Model directory not found: {modelPath}");
        }

        var predictionDir = context.Storage.GetPredictionDir(context.ScenarioId, context.JobId);
        if (!Directory.Exists(predictionDir))
        {
            throw new DirectoryNotFoundException($"Prediction directory not found: {predictionDir}");
        }

        var inputFiles = Directory.GetFiles(predictionDir, "input.*");
        if (inputFiles.Length == 0)
        {
            throw new FileNotFoundException($"No input file found in prediction directory: {predictionDir}");
        }

        return Task.CompletedTask;
    }

    private MLNetPredictRequest BuildRequest(IStep step, WorkflowContext context)
    {
        var modelId = step.Configuration!["model-id"].ToString()!;
        var predictionDir = context.Storage.GetPredictionDir(context.ScenarioId, context.JobId);
        var inputPath = Directory.GetFiles(predictionDir, "input.*")[0];
        var modelPath = Path.Combine(context.GetModelPath(modelId), "Model");

        var request = new MLNetPredictRequest
        {
            BasePath = context.Storage.GetScenarioBaseDir(context.ScenarioId),
            Command = "mlnet-predict",
            ModelPath = modelPath,
            InputPath = inputPath,
            OutputPath = context.Storage.GetPredictionResultPath(
                context.ScenarioId, context.JobId),
            Arguments = new Dictionary<string, object>
            {
                ["has-header"] = true
            },
            Context = context
        };

        return request;
    }

    private async Task<MLNetPredictProcessResult> ExecuteProcessAsync(
        MLNetPredictRequest request,
        IStep step,
        WorkflowContext context)
    {
        return await _processor.PredictAsync(request, context.CancellationToken);
    }
}