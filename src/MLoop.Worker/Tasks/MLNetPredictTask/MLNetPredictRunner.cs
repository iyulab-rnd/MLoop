using Microsoft.Extensions.Logging;
using MLoop.Models.Steps;
using MLoop.Worker.Pipeline;
using MLoop.Worker.Steps;
using MLoop.Worker.Tasks.MLNetTrainTask;

namespace MLoop.Worker.Tasks.MLNetPredictTask;

public class MLNetPredictRunner : IStepRunner
{
    private readonly MLNetPredictProcessor _processor;
    private readonly ILogger<MLNetPredictRunner> _logger;

    public string Type => "mlnet-predict";

    public MLNetPredictRunner(
        MLNetPredictProcessor processor,
        ILogger<MLNetPredictRunner> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task RunAsync(IStep step, WorkflowContext context)
    {
        await ValidateConfigurationAsync(step, context);
        var request = BuildRequest(step, context);
        await ExecuteProcessAsync(request, step, context);
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

        // 이미지 분류 예측인 경우 입력 파일 검증
        var predictionDir = context.Storage.GetPredictionDir(context.ScenarioId, context.JobId);
        if (!Directory.Exists(predictionDir))
        {
            throw new DirectoryNotFoundException($"Prediction directory not found: {predictionDir}");
        }

        // Variables에서 fileName 확인 (이미지 분류인 경우)
        if (context.Variables.TryGetValue("fileName", out var fileNameObj))
        {
            var fileName = fileNameObj.ToString();
            var filePath = Path.Combine(predictionDir, fileName!);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Input image file not found: {filePath}");
            }
        }

        return Task.CompletedTask;
    }

    private MLNetPredictRequest BuildRequest(IStep step, WorkflowContext context)
    {
        var modelId = step.Configuration!["model-id"].ToString()!;
        var modelPath = Path.Combine(context.GetModelPath(modelId), "Model");
        var predictionDir = context.Storage.GetPredictionDir(context.ScenarioId, context.JobId);

        // Get filename from job variables for image classification
        if (context.Variables.TryGetValue("fileName", out var fileNameObj))
        {
            var fileName = fileNameObj.ToString()!;
            var filePath = Path.Combine(predictionDir, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Image file not found: {filePath}");
            }

            return new MLNetPredictRequest
            {
                BasePath = context.Storage.GetScenarioBaseDir(context.ScenarioId),
                Command = "mlnet-predict",
                ModelPath = modelPath,
                InputPath = filePath,
                OutputPath = context.Storage.GetPredictionResultPath(
                    context.ScenarioId, context.JobId),
                Arguments = new Dictionary<string, object>
                {
                    ["has-header"] = false  // For image files, there is no header
                },
                Context = context
            };
        }

        // For non-image predictions, look for input.* files
        var inputFiles = Directory.GetFiles(predictionDir, "input.*");
        if (inputFiles.Length == 0)
        {
            throw new FileNotFoundException($"No input file found in prediction directory: {predictionDir}");
        }

        return new MLNetPredictRequest
        {
            BasePath = context.Storage.GetScenarioBaseDir(context.ScenarioId),
            Command = "mlnet-predict",
            ModelPath = modelPath,
            InputPath = inputFiles[0],
            OutputPath = context.Storage.GetPredictionResultPath(
                context.ScenarioId, context.JobId),
            Arguments = new Dictionary<string, object>
            {
                ["has-header"] = true
            },
            Context = context
        };
    }

    private async Task<MLNetPredictProcessResult> ExecuteProcessAsync(
        MLNetPredictRequest request,
        IStep step,
        WorkflowContext context)
    {
        return await _processor.PredictAsync(request, context.CancellationToken);
    }
}