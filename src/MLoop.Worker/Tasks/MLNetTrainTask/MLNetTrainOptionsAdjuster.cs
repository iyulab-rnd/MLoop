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

            // 검증 옵션 충돌 확인
            if (MLNetCommandOptions.HasValidationConflict(command, adjustedConfig.Args))
            {
                throw new ArgumentException(
                    "Only one validation method can be specified: cv-fold, split-ratio, or validation-dataset");
            }

            // 경로 검증 및 정규화
            ValidateAndNormalizePaths(command, adjustedConfig.Args);

            // 옵션값 검증 
            ValidateOptionValues(adjustedConfig.Args);

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
        // 검증 데이터 분할 방식이 지정되지 않은 경우에만 기본값 설정
        var hasValidationMethod = args.ContainsKey("validation-dataset") ||
                                args.ContainsKey("split-ratio") ||
                                args.ContainsKey("cv-fold");

        if (!hasValidationMethod)
        {
            args["split-ratio"] = "0.2"; // 기본 검증 세트 비율 20%
        }

        _logger.LogInformation("Applied traditional ML options adjustments");
    }

    private void AdjustTextClassificationOptions(Dictionary<string, object> args)
    {
        // batch-size 값 검증
        if (args.TryGetValue("batch-size", out var batchSize))
        {
            if (int.TryParse(batchSize.ToString(), out var size))
            {
                if (size < 1) args["batch-size"] = "1";
            }
        }

        // max-epoch 값 검증
        if (args.TryGetValue("max-epoch", out var maxEpoch))
        {
            if (int.TryParse(maxEpoch.ToString(), out var epochs))
            {
                if (epochs < 1) args["max-epoch"] = "1";
            }
        }

        _logger.LogInformation("Applied text classification options adjustments");
    }

    private void AdjustImageClassificationOptions(Dictionary<string, object> args)
    {
        // 이미지 크기 값 검증
        foreach (var dim in new[] { "image-width", "image-height" })
        {
            if (args.TryGetValue(dim, out var value))
            {
                if (int.TryParse(value.ToString(), out var size))
                {
                    if (size < 32) args[dim] = "32"; // 최소 이미지 크기 제한
                }
            }
        }

        _logger.LogInformation("Applied image classification options adjustments");
    }

    private void AdjustObjectDetectionOptions(Dictionary<string, object> args)
    {
        // 임계값 범위 검증
        foreach (var threshold in new[] { "score-threshold", "iou-threshold" })
        {
            if (args.TryGetValue(threshold, out var value))
            {
                if (float.TryParse(value.ToString(), out var thresholdValue))
                {
                    if (thresholdValue < 0) args[threshold] = "0";
                    if (thresholdValue > 1) args[threshold] = "1";
                }
            }
        }

        // width/height 값 검증 
        foreach (var dim in new[] { "width", "height" })
        {
            if (args.TryGetValue(dim, out var value))
            {
                if (int.TryParse(value.ToString(), out var size))
                {
                    if (size < 32) args[dim] = "32"; // 최소 크기 제한
                }
            }
        }

        // batch-size 값 검증
        if (args.TryGetValue("batch-size", out var batchSize))
        {
            if (int.TryParse(batchSize.ToString(), out var size))
            {
                if (size < 1) args["batch-size"] = "1";
            }
        }

        // epoch 값 검증
        if (args.TryGetValue("epoch", out var epoch))
        {
            if (int.TryParse(epoch.ToString(), out var epochs))
            {
                if (epochs < 1) args["epoch"] = "1";
            }
        }

        _logger.LogInformation("Applied object detection options adjustments");
    }

    private void AdjustRecommendationOptions(Dictionary<string, object> args)
    {
        // 검증 데이터 분할 방식이 지정되지 않은 경우에만 기본값 설정
        var hasValidationMethod = args.ContainsKey("validation-dataset") ||
                                args.ContainsKey("split-ratio") ||
                                args.ContainsKey("cv-fold");

        if (!hasValidationMethod)
        {
            args["split-ratio"] = "0.2";
        }

        _logger.LogInformation("Applied recommendation options adjustments");
    }

    private void AdjustForecastingOptions(Dictionary<string, object> args)
    {
        // horizon 값 검증
        if (args.TryGetValue("horizon", out var horizon))
        {
            if (int.TryParse(horizon.ToString(), out var periods))
            {
                if (periods < 1) args["horizon"] = "1";
            }
        }

        // 검증 데이터 분할 방식이 지정되지 않은 경우에만 기본값 설정
        var hasValidationMethod = args.ContainsKey("validation-dataset") ||
                                args.ContainsKey("split-ratio") ||
                                args.ContainsKey("cv-fold");

        if (!hasValidationMethod)
        {
            args["split-ratio"] = "0.2";
        }

        _logger.LogInformation("Applied forecasting options adjustments");
    }
}