using Microsoft.Extensions.Logging;
using MLoop.Worker.Tasks.CmdTask;
using MLoop.Worker.Tasks.MLNetTrainTask;
using System.Text;

namespace MLoop.Worker.Tasks.MLNetPredictTask;

public class MLNetPredictProcessor : CmdProcessorBase<MLNetPredictProcessResult>
{
    public MLNetPredictProcessor(
        ILogger<MLNetPredictProcessor> logger,
        string? mlnetPredictPath = null)
        : base(logger, mlnetPredictPath ?? "mlnet-predict")
    {
    }

    public async Task<MLNetPredictProcessResult> PredictAsync(
        MLNetPredictRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePredictRequest(request);
        string args = BuildPredictCommandLineArgs(request);
        return await RunProcessAsync(request, args, cancellationToken);
    }

    private void ValidatePredictRequest(MLNetPredictRequest request)
    {
        // 경로 검증
        if (string.IsNullOrWhiteSpace(request.BasePath))
        {
            throw new ArgumentException("BasePath is required");
        }

        if (!Directory.Exists(request.ModelPath))
        {
            throw new DirectoryNotFoundException($"Model directory not found: {request.ModelPath}");
        }

        if (!File.Exists(request.InputPath))
        {
            throw new FileNotFoundException($"Input file not found: {request.InputPath}");
        }

        // 타임아웃 검증
        if (request.Timeout.HasValue && request.Timeout.Value <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be greater than zero");
        }

        Logger.LogInformation(
            "Predict request validation passed with model path: {ModelPath}, input: {InputPath}",
            request.ModelPath,
            request.InputPath);
    }

    private static string BuildPredictCommandLineArgs(MLNetPredictRequest request)
    {
        var args = new StringBuilder();

        // Required positional arguments
        args.Append($"\"{request.ModelPath}\" \"{request.InputPath}\"");

        // Output path
        args.Append($" -o \"{request.OutputPath}\"");

        // Additional arguments
        foreach (var (key, value) in request.Arguments)
        {
            if (value is bool boolValue)
            {
                args.Append($" --{key} {boolValue.ToString().ToLower()}");
            }
            else
            {
                args.Append($" --{key} \"{value}\"");
            }
        }

        return args.ToString();
    }
}