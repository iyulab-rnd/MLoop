using Microsoft.Extensions.Logging;
using MLoop.Models.Jobs;
using MLoop.Services;
using System.Diagnostics;
using System.Text;

namespace MLoop.Worker.Tasks.MLNet.Train;

public class MLNetTrainProcessor : MLNetProcessorBase<MLNetTrainResult>
{
    private readonly MLNetOptionValidator _optionValidator;

    public MLNetTrainProcessor(
        ILogger<MLNetTrainProcessor> logger,
        string? mlnetPath = null)
        : base(logger, mlnetPath ?? "mlnet")
    {
        _optionValidator = new MLNetOptionValidator(logger);
    }

    public async Task<MLNetTrainResult> TrainAsync(
        MLNetTrainRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("MLNet CLI Path: {CliPath}", _cliPath);

            var dataPath = PathHelper.GetDataPath(request.BasePath);
            _optionValidator.ValidateOptions(dataPath, request.Command, request.Arguments);

            string args = BuildTrainCommandLineArgs(request);
            _logger.LogInformation("Executing MLNet command: {CliPath} {Arguments}", _cliPath, args);

            var result = await RunProcessAsync(request, args, cancellationToken) as MLNetTrainResult
                ?? throw new InvalidOperationException("Invalid result type");

            if (result.Success)
            {
                var modelPath = Path.Combine(request.BasePath, "Model");
                if (Directory.Exists(modelPath))
                {
                    await result.ParseMetricsAsync(request.BasePath);
                    _logger.LogInformation(
                        "Successfully parsed {Count} metrics from training output",
                        result.Metrics?.Count ?? 0);
                }
                else
                {
                    _logger.LogWarning("Model directory not found at {ModelPath}", modelPath);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MLNet training");
            throw;
        }
    }

    private static string BuildTrainCommandLineArgs(MLNetTrainRequest request)
    {
        var args = new StringBuilder(request.Command);
        var logPath = Path.Combine(request.BasePath, "train.log");

        foreach (var (key, value) in request.Arguments)
        {
            if (key is "o" or "output" or "name" or "log-file-path")
                continue;

            if (value is bool boolValue)
            {
                args.Append($" --{key} {boolValue.ToString().ToLower()}");
            }
            else if (key == "verbosity")
            {
                args.Append($" -v {value}");
            }
            else
            {
                if (key == "cross-validation" && string.IsNullOrEmpty(value?.ToString()))
                    continue;

                args.Append($" --{key} \"{value}\"");
            }
        }

        args.Append($" --log-file-path \"{logPath}\"");
        args.Append($" -o \"{request.BasePath}\"");
        args.Append(" --name \"Model\"");

        return args.ToString();
    }
}