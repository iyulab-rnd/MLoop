using System.Collections.Immutable;

namespace MLoop.Worker.Tasks.MLNetTrainTask;

/// <summary>
/// MLNet CLI 명령어별 옵션 정의
/// </summary>
public static class MLNetCommandOptions
{
    public class RequiredOption
    {
        public string Name { get; }
        public string Description { get; }
        public string Type { get; }

        public RequiredOption(string name, string description, string type = "string")
        {
            Name = name;
            Description = description;
            Type = type;
        }
    }

    /// <summary>
    /// 각 명령어별 필수 옵션 정의
    /// </summary>
    public static readonly ImmutableDictionary<string, RequiredOption[]> RequiredOptionsByCommand =
        new Dictionary<string, RequiredOption[]>
        {
            ["classification"] = [
                new("dataset", "File path to single dataset or training dataset"),
                new("label-col", "Name or zero-based index of label column")
            ],
            ["regression"] = [
                new("dataset", "File path to single dataset or training dataset"),
                new("label-col", "Name or zero-based index of label column")
            ],
            ["recommendation"] = [
                new("dataset", "File path to single dataset or training dataset"),
                new("user-col", "Name or zero-based index of user column"),
                new("item-col", "Name or zero-based index of item column"),
                new("rating-col", "Name or zero-based index of rating column")
            ],
            ["text-classification"] = [
                new("dataset", "File path to single dataset or training dataset"),
                new("label-col", "Name or zero-based index of label column"),
                new("text-col", "Name or zero-based index of text column")
            ],
            ["image-classification"] = [
                new("dataset", "Path to local folder containing labelled sub-folders of images")
            ],
            ["forecasting"] = [
                new("dataset", "File path to single dataset or training dataset"),
                new("label-col", "Name or zero-based index of label column"),
                new("time-col", "Name or zero-based index of time column"),
                new("horizon", "Number of periods to forecast", "int")
            ],
            ["object-detection"] = [
                new("dataset", "File path to single dataset or training dataset")
            ]
        }.ToImmutableDictionary();

    /// <summary>
    /// 모든 명령어에 공통으로 적용되는 기본값
    /// </summary>
    public static readonly ImmutableDictionary<string, object> CommonDefaultOptions =
    new Dictionary<string, object>
    {
        ["name"] = "Model",           // 모델 이름 고정
        ["verbosity"] = "diag",       // 로깅 레벨 고정
        ["cache"] = "Auto",           // 캐시 설정 고정
        ["has-header"] = true,        // 헤더 존재 기본값
        ["allow-quote"] = false,      // 따옴표 허용 기본값
        ["read-multi-lines"] = true,  // 여러 줄 읽기 기본값
        ["log-file-path"] = "train.log"  // 로그 파일명
    }.ToImmutableDictionary();

    /// <summary>
    /// 각 명령어별 기본값 정의
    /// </summary>
    public static readonly ImmutableDictionary<string, ImmutableDictionary<string, object>> DefaultOptionsByCommand =
        new Dictionary<string, ImmutableDictionary<string, object>>
        {
            ["classification"] = new Dictionary<string, object>
            {
                ["train-time"] = "600",      // 10분
                ["split-ratio"] = "0.2",     // 20% 검증 데이터
                ["cross-validation"] = null,  // 기본값으로 사용하지 않음
                ["validation-dataset"] = null // 기본값으로 사용하지 않음
            }.ToImmutableDictionary(),

            ["regression"] = new Dictionary<string, object>
            {
                ["train-time"] = "600",
                ["split-ratio"] = "0.2",
                ["cross-validation"] = null,
                ["validation-dataset"] = null
            }.ToImmutableDictionary(),

            ["recommendation"] = new Dictionary<string, object>
            {
                ["train-time"] = "100",
                ["split-ratio"] = "0.2"
            }.ToImmutableDictionary(),

            ["text-classification"] = new Dictionary<string, object>
            {
                ["batch-size"] = "32",
                ["max-epoch"] = "50",
                ["device"] = "cpu",
                ["seed"] = "42",
                ["split-ratio"] = "0.2"
            }.ToImmutableDictionary(),

            ["image-classification"] = new Dictionary<string, object>
            {
                ["image-width"] = "224",
                ["image-height"] = "224",
                ["batch-size"] = "32",
                ["epoch"] = "100",
                ["device"] = "cpu"
            }.ToImmutableDictionary(),

            ["forecasting"] = new Dictionary<string, object>
            {
                ["train-time"] = "600",
                ["split-ratio"] = "0.2",
                ["horizon"] = "1"  // 기본 예측 기간
            }.ToImmutableDictionary(),

            ["object-detection"] = new Dictionary<string, object>
            {
                ["width"] = "800",
                ["height"] = "600",
                ["batch-size"] = "10",
                ["epoch"] = "5",
                ["device"] = "cpu",
                ["score-threshold"] = "0.5",
                ["iou-threshold"] = "0.5"
            }.ToImmutableDictionary()
        }.ToImmutableDictionary();

    /// <summary>
    /// 각 명령어별 검증 옵션 정의
    /// </summary>
    public static readonly ImmutableDictionary<string, string[]> ValidationOptions =
        new Dictionary<string, string[]>
        {
            ["classification"] = ["cv-fold", "split-ratio", "validation-dataset"],
            ["regression"] = ["cv-fold", "split-ratio", "validation-dataset"],
            ["recommendation"] = ["cv-fold", "split-ratio", "validation-dataset"],
            ["text-classification"] = ["split-ratio", "validation-dataset"],
            ["forecasting"] = ["split-ratio", "validation-dataset"]
        }.ToImmutableDictionary();

    /// <summary>
    /// 파일 경로를 포함하는 옵션들
    /// </summary>
    public static readonly ImmutableHashSet<string> PathOptions =
        new HashSet<string>
        {
            "dataset",
            "validation-dataset",
            "test-dataset",
            "log-file-path",
            "output"
        }.ToImmutableHashSet();

    /// <summary>
    /// 숫자 범위가 있는 옵션 정의
    /// </summary>
    public static readonly ImmutableDictionary<string, (double Min, double Max)> NumericRangeOptions =
        new Dictionary<string, (double Min, double Max)>
        {
            ["split-ratio"] = (0.0, 1.0),
            ["score-threshold"] = (0.0, 1.0),
            ["iou-threshold"] = (0.0, 1.0),
            ["train-time"] = (1, double.MaxValue),
            ["cv-fold"] = (2, 100),
            ["batch-size"] = (1, 1000),
            ["max-epoch"] = (1, 1000),
            ["epoch"] = (1, 1000)
        }.ToImmutableDictionary();

    /// <summary>
    /// 유효한 옵션값 목록이 있는 경우
    /// </summary>
    public static readonly ImmutableDictionary<string, ImmutableHashSet<string>> ValidOptionValues =
        new Dictionary<string, ImmutableHashSet<string>>
        {
            ["cache"] = new HashSet<string> { "Auto", "On", "Off" }.ToImmutableHashSet(),
            ["device"] = new HashSet<string> { "cpu", "cuda:0", "cuda:1", "cuda:2", "cuda:3" }.ToImmutableHashSet(),
            ["verbosity"] = new HashSet<string> { "q", "quiet", "m", "minimal", "diag", "diagnostic" }.ToImmutableHashSet()
        }.ToImmutableDictionary();

    /// <summary>
    /// 명령어별 디렉토리 검증이 필요한 옵션
    /// </summary>
    public static readonly ImmutableDictionary<string, string[]> DirectoryOptions =
        new Dictionary<string, string[]>
        {
            ["image-classification"] = ["dataset"],
            ["object-detection"] = ["dataset"]
        }.ToImmutableDictionary();

    /// <summary>
    /// 명령어별 파일 검증이 필요한 옵션
    /// </summary>
    public static readonly ImmutableDictionary<string, string[]> FileOptions =
        new Dictionary<string, string[]>
        {
            ["classification"] = ["dataset", "validation-dataset"],
            ["regression"] = ["dataset", "validation-dataset"],
            ["recommendation"] = ["dataset", "validation-dataset"],
            ["text-classification"] = ["dataset", "validation-dataset"],
            ["forecasting"] = ["dataset", "validation-dataset"]
        }.ToImmutableDictionary();

    /// <summary>
    /// MLNet CLI에서 지원하는 명령어 목록
    /// </summary>
    public static readonly ImmutableHashSet<string> SupportedCommands =
        new HashSet<string>
        {
            "classification",
            "regression",
            "recommendation",
            "image-classification",
            "text-classification",
            "forecasting",
            "object-detection"
        }.ToImmutableHashSet();

    /// <summary>
    /// 주어진 명령어가 지원되는지 확인
    /// </summary>
    public static bool IsCommandSupported(string command) =>
        SupportedCommands.Contains(command.ToLowerInvariant());

    /// <summary>
    /// 주어진 명령어에 대한 필수 옵션 목록 반환
    /// </summary>
    public static IEnumerable<RequiredOption> GetRequiredOptions(string command) =>
        RequiredOptionsByCommand.TryGetValue(command.ToLowerInvariant(), out var options)
            ? options
            : Array.Empty<RequiredOption>();

    /// <summary>
    /// 주어진 명령어에 대한 기본 옵션값 목록 반환
    /// </summary>
    public static ImmutableDictionary<string, object> GetDefaultOptions(string command)
    {
        var defaults = new Dictionary<string, object>(CommonDefaultOptions);

        if (DefaultOptionsByCommand.TryGetValue(command.ToLowerInvariant(), out var commandDefaults))
        {
            foreach (var (key, value) in commandDefaults)
            {
                defaults[key] = value;
            }
        }

        return defaults.ToImmutableDictionary();
    }

    /// <summary>
    /// 주어진 옵션값이 유효한지 검증
    /// </summary>
    public static bool IsValidOptionValue(string optionName, string value)
    {
        if (ValidOptionValues.TryGetValue(optionName, out var validValues))
        {
            return validValues.Contains(value);
        }

        // 범위가 있는 숫자 옵션 검증
        if (NumericRangeOptions.TryGetValue(optionName, out var range))
        {
            if (double.TryParse(value, out var numValue))
            {
                return numValue >= range.Min && numValue <= range.Max;
            }
            return false;
        }

        return true;  // 별도 검증 규칙이 없는 경우
    }

    /// <summary>
    /// 주어진 명령어의 검증 옵션들이 상호 배타적인지 검증
    /// </summary>
    public static bool HasValidationConflict(string command, Dictionary<string, object> options)
    {
        if (ValidationOptions.TryGetValue(command.ToLowerInvariant(), out var validationOpts))
        {
            return validationOpts.Count(opt => options.ContainsKey(opt)) > 1;
        }
        return false;
    }

    /// <summary>
    /// 주어진 명령어에 대해 디렉토리 검증이 필요한 옵션 목록 반환
    /// </summary>
    public static IEnumerable<string> GetDirectoryOptions(string command) =>
        DirectoryOptions.TryGetValue(command.ToLowerInvariant(), out var options)
            ? options
            : Array.Empty<string>();

    /// <summary>
    /// 주어진 명령어에 대해 파일 검증이 필요한 옵션 목록 반환
    /// </summary>
    public static IEnumerable<string> GetFileOptions(string command) =>
        FileOptions.TryGetValue(command.ToLowerInvariant(), out var options)
            ? options
            : Array.Empty<string>();
}