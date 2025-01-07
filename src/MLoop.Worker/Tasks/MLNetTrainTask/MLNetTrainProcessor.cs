using Microsoft.Extensions.Logging;
using MLoop.Worker.Tasks.CmdTask;
using System.Text;

namespace MLoop.Worker.Tasks.MLNetTrainTask;

public class MLNetTrainProcessor : CmdProcessorBase<MLNetTrainProcessResult>
{
    private static readonly HashSet<string> SupportedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "classification",
        "regression",
        "recommendation",
        "image-classification",
        "text-classification",
        "forecasting",
        "object-detection"
    };

    private static readonly Dictionary<string, HashSet<string>> RequiredArgumentsByCommand = new()
    {
        ["classification"] = ["dataset", "label-col"],
        ["regression"] = ["dataset", "label-col"],
        ["recommendation"] = ["dataset", "user-col", "item-col", "rating-col"],
        ["image-classification"] = ["dataset"],
        ["text-classification"] = ["dataset", "text-col", "label-col"],
        ["forecasting"] = ["dataset", "time-col", "label-col", "horizon"],
        ["object-detection"] = ["dataset"]
    };

    private static readonly HashSet<string> DirectoryDatasetCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "image-classification",  // 레이블된 서브폴더들이 있는 디렉토리
        "object-detection"       // 이미지와 레이블 데이터가 있는 디렉토리
    };

    private static readonly HashSet<string> DatasetArgKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "dataset",
        "validation-dataset",  // 검증용 데이터셋 
        "test-dataset"        // 테스트용 데이터셋
    };

    private static readonly HashSet<string> PathArgKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "dataset",
        "validation-dataset",
        "test-dataset",
        "o",
        "output",
        "output-path",
        "log-file-path"
    };

    public MLNetTrainProcessor(
        ILogger<MLNetTrainProcessor> logger,
        string? mlnetPath = null)
        : base(logger, mlnetPath ?? "mlnet")
    {
    }

    public async Task<MLNetTrainProcessResult> TrainAsync(
        MLNetTrainRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("MLNet CLI Path: {CliPath}", CliPath);

            Logger.LogInformation("Training request - Command: {Command}, BasePath: {BasePath}",
                request.Command, request.BasePath);
            Logger.LogInformation("Arguments: {Arguments}",
                string.Join(", ", request.Arguments.Select(kv => $"{kv.Key}={kv.Value}")));

            ValidateTrainRequest(request);
            string args = BuildTrainCommandLineArgs(request);

            Logger.LogInformation("Executing MLNet command: {CliPath} {Arguments}", CliPath, args);

            var result = await RunProcessAsync(request, args, cancellationToken);

            Logger.LogInformation("Process completed with exit code: {ExitCode}", result.ExitCode);
            if (!string.IsNullOrEmpty(result.StandardOutput))
                Logger.LogInformation("Standard output: {Output}", result.StandardOutput);
            if (!string.IsNullOrEmpty(result.StandardError))
                Logger.LogWarning("Standard error: {Error}", result.StandardError);

            if (result.Success)
            {
                var modelPath = Path.Combine(request.BasePath, "Model");
                if (Directory.Exists(modelPath))
                {
                    try
                    {
                        await result.ParseAsync(request.BasePath);
                        Logger.LogInformation(
                            "Successfully parsed {Count} metrics from training output",
                            result.Metrics?.Count ?? 0);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error parsing training metrics");
                    }
                }
                else
                {
                    Logger.LogWarning("Model directory not found at {ModelPath}", modelPath);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing MLNet training");
            return new MLNetTrainProcessResult
            {
                Success = false,
                ExitCode = -1,
                Exception = ex,
                StandardError = ex.Message,
                ProcessingTime = TimeSpan.Zero
            };
        }
    }

    private void ValidateTrainRequest(MLNetTrainRequest request)
    {
        ValidateCommand(request.Command);
        ValidateRequiredArguments(request.Command, request.Arguments);
        ValidateBasePath(request.BasePath);
        NormalizeAndValidatePaths(request);

        Logger.LogInformation(
            "Train request validation passed for command: {Command} with arguments: {Arguments}",
            request.Command,
            string.Join(", ", request.Arguments.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    private void ValidateCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command is required for training");
        }

        if (!SupportedCommands.Contains(command))
        {
            throw new ArgumentException(
                $"Unsupported command: {command}. Supported commands: {string.Join(", ", SupportedCommands)}");
        }
    }

    private void ValidateRequiredArguments(string command, Dictionary<string, object> arguments)
    {
        if (RequiredArgumentsByCommand.TryGetValue(command, out var requiredArgs))
        {
            var missingArgs = requiredArgs
                .Where(arg => !arguments.ContainsKey(arg))
                .ToList();

            if (missingArgs.Count > 0)
            {
                throw new ArgumentException(
                    $"Missing required arguments for {command}: {string.Join(", ", missingArgs)}");
            }
        }
    }

    private void ValidateBasePath(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("BasePath is required");
        }

        try
        {
            IOHelper.EnsureDirectoryExists(basePath);
            Logger.LogInformation("Created output directory: {BasePath}", basePath);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Cannot create output directory: {basePath}", ex);
        }
    }

    private void NormalizeAndValidatePaths(MLNetTrainRequest request)
    {
        // 새로운 Dictionary를 할당하는 대신 기존 항목들을 수정
        foreach (var (key, value) in request.Arguments.ToList())
        {
            if (value is string strValue && PathArgKeys.Any(pathKey => key.EndsWith(pathKey, StringComparison.OrdinalIgnoreCase)))
            {
                request.Arguments[key] = IOHelper.NormalizePath(strValue);
            }
        }

        // dataset 경로들 검증
        foreach (var (key, value) in request.Arguments)
        {
            foreach (var datasetKey in DatasetArgKeys)
            {
                if (key.EndsWith(datasetKey, StringComparison.OrdinalIgnoreCase) &&
                    value is string datasetPath)
                {
                    ValidateDatasetPath(request.Command, key, datasetPath);
                }
            }
        }
    }

    private static string BuildTrainCommandLineArgs(MLNetTrainRequest request)
    {
        var args = new StringBuilder(request.Command);
        var logPath = IOHelper.NormalizePath(Path.Combine(request.BasePath, "train.log"));

        foreach (var (key, value) in request.Arguments)
        {
            if (key is "o" or "output" or "output-path" or "name" or "log-file-path")
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

    private void ValidateDatasetPath(string command, string argKey, string path)
    {
        bool isDirectoryDataset = DirectoryDatasetCommands.Contains(command);
        bool isValidationOrTest = argKey != "dataset";

        try
        {
            var normalizedPath = IOHelper.NormalizePath(path);

            if (isDirectoryDataset)
            {
                IOHelper.EnsureDirectoryExists(normalizedPath, argKey);

                if (!isValidationOrTest &&
                    command.Equals("image-classification", StringComparison.OrdinalIgnoreCase))
                {
                    IOHelper.ValidateImageClassificationDirectory(
                        normalizedPath,
                        message => Logger.LogWarning(message));
                }
            }
            else
            {
                IOHelper.ValidateDatasetFile(
                    normalizedPath,
                    argKey,
                    (format, args) => Logger.LogWarning(format, args));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating {ArgKey} path: {Path}", argKey, path);
            throw;
        }
    }
}