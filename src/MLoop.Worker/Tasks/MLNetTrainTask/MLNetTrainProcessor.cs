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
            // MLNet CLI 경로 로깅
            Logger.LogInformation("MLNet CLI Path: {CliPath}", CliPath);

            // 요청 파라미터 로깅
            Logger.LogInformation("Training request - Command: {Command}, BasePath: {BasePath}",
                request.Command, request.BasePath);
            Logger.LogInformation("Arguments: {Arguments}",
                string.Join(", ", request.Arguments.Select(kv => $"{kv.Key}={kv.Value}")));

            ValidateTrainRequest(request);
            string args = BuildTrainCommandLineArgs(request);

            // 실행할 명령어 로깅
            Logger.LogInformation("Executing MLNet command: {CliPath} {Arguments}", CliPath, args);

            var result = await RunProcessAsync(request, args, cancellationToken);

            // 프로세스 실행 결과 로깅
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
        if (string.IsNullOrWhiteSpace(request.Command))
        {
            throw new ArgumentException("Command is required for training");
        }

        if (!SupportedCommands.Contains(request.Command))
        {
            throw new ArgumentException(
                $"Unsupported command: {request.Command}. Supported commands: {string.Join(", ", SupportedCommands)}");
        }

        // Required Arguments 검증
        if (RequiredArgumentsByCommand.TryGetValue(request.Command, out var requiredArgs))
        {
            var missingArgs = requiredArgs
                .Where(arg => !request.Arguments.ContainsKey(arg))
                .ToList();

            if (missingArgs.Count > 0)
            {
                throw new ArgumentException(
                    $"Missing required arguments for {request.Command}: {string.Join(", ", missingArgs)}");
            }
        }

        // BasePath 검증
        if (string.IsNullOrWhiteSpace(request.BasePath))
        {
            throw new ArgumentException("BasePath is required");
        }

        // 데이터셋 파일 경로 검증
        foreach (var (key, value) in request.Arguments)
        {
            if (key.EndsWith("dataset", StringComparison.OrdinalIgnoreCase) &&
                value is string datasetPath &&
                !File.Exists(datasetPath))
            {
                throw new FileNotFoundException(
                    $"Dataset file not found for argument '{key}': {datasetPath}");
            }
        }

        Logger.LogInformation(
            "Train request validation passed for command: {Command} with arguments: {Arguments}",
            request.Command,
            string.Join(", ", request.Arguments.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    private static string BuildTrainCommandLineArgs(MLNetTrainRequest request)
    {
        var args = new StringBuilder(request.Command);

        // 로그 파일 경로 설정
        var logPath = Path.Combine(request.BasePath, "train.log");

        foreach (var (key, value) in request.Arguments)
        {
            // output path와 name은 마지막에 별도로 처리
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
                // cross-validation이 빈 문자열이면 건너뛰기
                if (key == "cross-validation" && string.IsNullOrEmpty(value?.ToString()))
                    continue;

                args.Append($" --{key} \"{value}\"");
            }
        }

        // 출력 관련 옵션들은 마지막에 추가
        args.Append($" --log-file-path \"{logPath}\"");  // 변경: --log -> --log-file-path
        args.Append($" -o \"{request.BasePath}\"");  // 출력 경로
        args.Append(" --name \"Model\"");  // 모델 이름

        return args.ToString();
    }
}