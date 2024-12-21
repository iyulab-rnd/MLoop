using Microsoft.Extensions.Logging;
using MLoop.Worker.Tasks.CmdTask;

namespace MLoop.Worker.Tasks.MLNetTrainTask;

public class MLNetTrainOptionsAdjuster
{
    private readonly ILogger _logger;

    public MLNetTrainOptionsAdjuster(ILogger logger)
    {
        _logger = logger;
    }

    public CmdConfig AdjustOptions(CmdConfig originalConfig)
    {
        var command = originalConfig.Command.ToLowerInvariant();

        if (!MLNetCommandOptions.IsCommandSupported(command))
        {
            throw new ArgumentException($"Unsupported command: {command}");
        }

        var adjustedConfig = new CmdConfig(command, new Dictionary<string, object>(originalConfig.Args));

        try
        {
            // 필수 옵션 검증
            ValidateRequiredOptions(command, adjustedConfig.Args);

            // 기본값 적용
            ApplyDefaultOptions(command, adjustedConfig.Args);

            // 경로 검증 및 정규화
            ValidateAndNormalizePaths(command, adjustedConfig.Args);

            // 옵션값 검증 
            ValidateOptionValues(adjustedConfig.Args);

            // 검증 옵션 충돌 확인
            if (MLNetCommandOptions.HasValidationConflict(command, adjustedConfig.Args))
            {
                throw new ArgumentException(
                    "Only one validation method can be specified: cv-fold, split-ratio, or validation-dataset");
            }

            // 명령어별 추가 처리
            AdjustCommandSpecificOptions(command, adjustedConfig.Args);

            _logger.LogInformation(
                "Successfully adjusted options for {Command}: {Options}",
                command,
                string.Join(", ", adjustedConfig.Args.Select(kv => $"{kv.Key}={kv.Value}")));

            return adjustedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error adjusting options for command {Command} with arguments: {Arguments}",
                command,
                string.Join(", ", originalConfig.Args.Select(kv => $"{kv.Key}={kv.Value}")));
            throw;
        }
    }

    private void ValidateRequiredOptions(string command, Dictionary<string, object> args)
    {
        var requiredOptions = MLNetCommandOptions.GetRequiredOptions(command);
        var missingOptions = requiredOptions
            .Where(opt => !args.ContainsKey(opt.Name))
            .ToList();

        if (missingOptions.Any())
        {
            var missingList = string.Join(", ", missingOptions.Select(opt => $"{opt.Name} ({opt.Description})"));
            throw new ArgumentException($"Missing required options for {command}: {missingList}");
        }
    }

    private void ApplyDefaultOptions(string command, Dictionary<string, object> args)
    {
        var defaults = MLNetCommandOptions.GetDefaultOptions(command);

        // validation 관련 옵션이 이미 있는지 확인
        bool hasValidationOption = args.ContainsKey("validation-dataset") ||
                                 args.ContainsKey("cv-fold") ||
                                 args.ContainsKey("split-ratio");

        foreach (var (key, value) in defaults)
        {
            // validation 관련 옵션은 사용자가 지정한 것이 없을 때만 기본값 적용
            if ((key == "validation-dataset" || key == "cv-fold" || key == "split-ratio"))
            {
                if (!hasValidationOption)
                {
                    args[key] = value;
                }
                continue;
            }

            // 다른 옵션들은 기존처럼 처리
            if (!args.ContainsKey(key))
            {
                args[key] = value;
            }
        }
    }

    private void ValidateAndNormalizePaths(string command, Dictionary<string, object> args)
    {
        // 파일 검증이 필요한 옵션들 확인
        foreach (var option in MLNetCommandOptions.GetFileOptions(command))
        {
            if (args.TryGetValue(option, out var path) && path is string filePath)
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(
                        $"File not found for option '{option}': {filePath}");
                }
                args[option] = Path.GetFullPath(filePath);
            }
        }

        // 디렉토리 검증이 필요한 옵션들 확인
        foreach (var option in MLNetCommandOptions.GetDirectoryOptions(command))
        {
            if (args.TryGetValue(option, out var path) && path is string dirPath)
            {
                if (!Directory.Exists(dirPath))
                {
                    throw new DirectoryNotFoundException(
                        $"Directory not found for option '{option}': {dirPath}");
                }
                args[option] = Path.GetFullPath(dirPath);
            }
        }
    }

    private void ValidateOptionValues(Dictionary<string, object> args)
    {
        foreach (var (key, value) in args)
        {
            if (value == null) continue;

            var strValue = value.ToString()!;

            if (!MLNetCommandOptions.IsValidOptionValue(key, strValue))
            {
                if (MLNetCommandOptions.NumericRangeOptions.TryGetValue(key, out var range))
                {
                    throw new ArgumentException(
                        $"Invalid value for option '{key}': {value}. Must be between {range.Min} and {range.Max}");
                }

                if (MLNetCommandOptions.ValidOptionValues.TryGetValue(key, out var validValues))
                {
                    throw new ArgumentException(
                        $"Invalid value for option '{key}': {value}. Must be one of: {string.Join(", ", validValues)}");
                }

                throw new ArgumentException($"Invalid value for option '{key}': {value}");
            }
        }
    }

    private void AdjustCommandSpecificOptions(string command, Dictionary<string, object> args)
    {
        switch (command)
        {
            case "classification":
            case "regression":
                AdjustTraditionalMlOptions(args);
                break;

            case "text-classification":
                AdjustTextClassificationOptions(args);
                break;

            case "image-classification":
                AdjustImageClassificationOptions(args);
                break;

            case "object-detection":
                AdjustObjectDetectionOptions(args);
                break;

            case "recommendation":
                AdjustRecommendationOptions(args);
                break;

            case "forecasting":
                AdjustForecastingOptions(args);
                break;
        }
    }

    private void AdjustTraditionalMlOptions(Dictionary<string, object> args)
    {
        // 검증 데이터 분할 방식이 지정되지 않은 경우 기본값 설정
        if (!args.ContainsKey("validation-dataset") &&
            !args.ContainsKey("split-ratio") &&
            !args.ContainsKey("cv-fold"))
        {
            args["split-ratio"] = "0.2";
        }

        // 기타 특수한 처리가 필요한 경우 여기에 추가
        _logger.LogInformation("Applied traditional ML options adjustments");
    }

    private void AdjustTextClassificationOptions(Dictionary<string, object> args)
    {
        if (args.TryGetValue("batch-size", out var batchSize))
        {
            if (!int.TryParse(batchSize.ToString(), out var size) || size <= 0)
            {
                throw new ArgumentException($"Invalid batch size: {batchSize}. Must be a positive integer.");
            }
        }

        if (args.TryGetValue("max-epoch", out var maxEpoch))
        {
            if (!int.TryParse(maxEpoch.ToString(), out var epochs) || epochs <= 0)
            {
                throw new ArgumentException($"Invalid max epoch: {maxEpoch}. Must be a positive integer.");
            }
        }

        _logger.LogInformation("Applied text classification options adjustments");
    }

    private void AdjustImageClassificationOptions(Dictionary<string, object> args)
    {
        foreach (var dim in new[] { "image-width", "image-height" })
        {
            if (args.TryGetValue(dim, out var value))
            {
                if (!int.TryParse(value.ToString(), out var size) || size <= 0)
                {
                    throw new ArgumentException($"Invalid {dim}: {value}. Must be a positive integer.");
                }
            }
        }

        _logger.LogInformation("Applied image classification options adjustments");
    }

    private void AdjustObjectDetectionOptions(Dictionary<string, object> args)
    {
        foreach (var threshold in new[] { "score-threshold", "iou-threshold" })
        {
            if (args.TryGetValue(threshold, out var value))
            {
                if (!float.TryParse(value.ToString(), out var thresholdValue) ||
                    thresholdValue < 0 || thresholdValue > 1)
                {
                    throw new ArgumentException(
                        $"Invalid {threshold}: {value}. Must be between 0 and 1.");
                }
            }
        }

        _logger.LogInformation("Applied object detection options adjustments");
    }

    private void AdjustRecommendationOptions(Dictionary<string, object> args)
    {
        // 추천 시스템 특화 옵션 처리
        // 예: 사용자-아이템 상호작용 데이터 검증 등
        _logger.LogInformation("Applied recommendation options adjustments");
    }

    private void AdjustForecastingOptions(Dictionary<string, object> args)
    {
        if (args.TryGetValue("horizon", out var horizon))
        {
            if (!int.TryParse(horizon.ToString(), out var periods) || periods <= 0)
            {
                throw new ArgumentException(
                    $"Invalid horizon: {horizon}. Must be a positive integer.");
            }
        }

        _logger.LogInformation("Applied forecasting options adjustments");
    }
}