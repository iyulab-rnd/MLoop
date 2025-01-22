using Microsoft.Extensions.Logging;
using MLoop.Models.Jobs;

namespace MLoop.Worker.Tasks.MLNet;

public class MLNetOptionValidator
{
    private readonly ILogger _logger;
    
    public MLNetOptionValidator(ILogger logger)
    {
        _logger = logger;
    }

    public void ValidateOptions(string dataPath, string command, Dictionary<string, object> arguments)
    {
        // 명령어 검증
        if (!IsCommandSupported(command))
        {
            throw new JobProcessException(
                JobFailureType.ConfigurationError,
                $"Unsupported command: {command}");
        }

        // 필수 옵션 검증
        ValidateRequiredOptions(command, arguments);

        // 경로 검증
        ValidatePaths(dataPath, command, arguments);

        // 옵션값 검증
        ValidateOptionValues(arguments);

        _logger.LogInformation(
            "Successfully validated options for {Command}: {Options}",
            command,
            string.Join(", ", arguments.Select(kv => $"{kv.Key}={kv.Value}")));
    }

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

    private static bool IsCommandSupported(string command) =>
        SupportedCommands.Contains(command);

    private static readonly Dictionary<string, HashSet<string>> RequiredOptionsByCommand = new()
    {
        ["classification"] = new() { "dataset", "label-col" },
        ["regression"] = new() { "dataset", "label-col" },
        ["recommendation"] = new() { "dataset", "user-col", "item-col", "rating-col" },
        ["text-classification"] = new() { "dataset", "text-col", "label-col" },
        ["image-classification"] = new() { "dataset" },
        ["forecasting"] = new() { "dataset", "time-col", "label-col", "horizon" },
        ["object-detection"] = new() { "dataset" }
    };

    private void ValidateRequiredOptions(string command, Dictionary<string, object> arguments)
    {
        if (RequiredOptionsByCommand.TryGetValue(command, out var requiredOptions))
        {
            var missingOptions = requiredOptions
                .Where(opt => !arguments.ContainsKey(opt))
                .ToList();

            if (missingOptions.Any())
            {
                throw new JobProcessException(
                    JobFailureType.ConfigurationError,
                    $"Missing required options for {command}: {string.Join(", ", missingOptions)}");
            }
        }
    }

    private static readonly HashSet<string> PathOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "dataset",
        "validation-dataset",
        "test-dataset"
    };

    private void ValidatePaths(string dataPath, string command, Dictionary<string, object> arguments)
    {
        foreach (var (key, value) in arguments)
        {
            if (PathOptions.Contains(key) && value is string path)
            {
                string fullPath;

                // 1. path가 절대경로인지 확인
                if (Path.IsPathRooted(path))
                {
                    fullPath = Path.GetFullPath(path);
                }
                // 2. 상대경로인 경우 dataPath와 결합
                else
                {
                    fullPath = Path.GetFullPath(Path.Combine(dataPath, path));
                }

                // 3&4. 파일 또는 폴더 존재 체크
                bool isValid = false;
                FileAttributes attr = File.GetAttributes(fullPath);

                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    // 폴더인 경우
                    isValid = Directory.Exists(fullPath);
                }
                else
                {
                    // 파일인 경우
                    isValid = File.Exists(fullPath);
                }

                // 5&6. 유효성 체크 및 업데이트 또는 예외 발생
                if (isValid)
                {
                    // 5. 유효한 경우 절대경로로 업데이트
                    arguments[key] = fullPath;
                }
                else
                {
                    // 6. 유효하지 않은 경우 예외 발생
                    throw new JobProcessException(
                        JobFailureType.FileNotFound,
                        $"Path not found for option '{key}': {fullPath}");
                }
            }
        }
    }

    private static readonly Dictionary<string, (double Min, double Max)> NumericRanges = new()
    {
        ["split-ratio"] = (0.0, 1.0),
        ["score-threshold"] = (0.0, 1.0),
        ["iou-threshold"] = (0.0, 1.0),
        ["train-time"] = (1, double.MaxValue),
        ["cv-fold"] = (2, 100),
        ["batch-size"] = (1, 1000),
        ["max-epoch"] = (1, 1000),
        ["epoch"] = (1, 1000)
    };
    
    private void ValidateOptionValues(Dictionary<string, object> arguments)
    {
        foreach (var (key, value) in arguments)
        {
            if (NumericRanges.TryGetValue(key, out var range))
            {
                if (double.TryParse(value.ToString(), out var numValue))
                {
                    if (numValue < range.Min || numValue > range.Max)
                    {
                        throw new JobProcessException(
                            JobFailureType.ConfigurationError,
                            $"Value for '{key}' must be between {range.Min} and {range.Max}. Got: {value}");
                    }
                }
            }
        }
    }
}