using Microsoft.Extensions.Logging;
using MLoop.Models.Workflows;
using MLoop.Worker.Pipeline;
using MLoop.Worker.Tasks.MLNet.Predict;

namespace MLoop.Worker.Tasks.MLNet.StepRunners;

public class MLNetPredictStepRunner : IStepRunner
{
    private readonly MLNetPredictProcessor _processor;
    private readonly ILogger<MLNetPredictStepRunner> _logger;

    public string Type => "mlnet-predict";

    public MLNetPredictStepRunner(
        MLNetPredictProcessor processor,
        ILogger<MLNetPredictStepRunner> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task RunAsync(WorkflowStep step, JobContext context)
    {
        await ValidateConfigurationAsync(step, context);
        var request = BuildRequest(step, context);
        await ExecuteProcessAsync(request, step, context);
    }

    private void ValidateConfiguration(WorkflowStep step, JobContext context)
    {
        if (string.IsNullOrEmpty(context.ModelId))
        {
            throw new ArgumentException("model-id is required for prediction step");
        }

        var modelId = context.ModelId;
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
    }

    private Task ValidateConfigurationAsync(WorkflowStep step, JobContext context)
    {
        ValidateConfiguration(step, context);
        return Task.CompletedTask;
    }

    private MLNetPredictRequest BuildRequest(WorkflowStep step, JobContext context)
    {
        var modelId = context.ModelId ?? throw new InvalidOperationException("ModelId not found in context");
        var modelPath = Path.Combine(context.GetModelPath(modelId), "Model");
        var predictionDir = context.Storage.GetPredictionDir(context.ScenarioId, context.JobId);

        // Get filename from job variables for image classification
        if (context.Variables.TryGetValue("fileName", out var fileNameObj))
        {
            var fileName = fileNameObj.ToString()!;
            var filePath = Path.Combine(predictionDir, fileName);

            return new MLNetPredictRequest
            {
                BasePath = context.Storage.GetScenarioBaseDir(context.ScenarioId),
                Command = "predict",
                ModelPath = modelPath,
                InputPath = filePath,
                OutputPath = context.Storage.GetPredictionResultPath(
                    context.ScenarioId, context.JobId),
                Arguments = new Dictionary<string, object>
                {
                    ["has-header"] = false
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
            Command = "predict",
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

    private async Task<MLNetPredictResult> ExecuteProcessAsync(
        MLNetPredictRequest request,
        WorkflowStep step,
        JobContext context)
    {
        return await _processor.PredictAsync(request, context.CancellationToken);
    }
}