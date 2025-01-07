namespace MLoop;

public static class IOHelper
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png" };
    private static readonly string[] DataExtensions = { ".csv", ".txt", ".tsv" };

    /// <summary>
    /// 경로를 정규화하고 forward slash로 변환
    /// </summary>
    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace("\\", "/");
    }

    /// <summary>
    /// 디렉토리가 존재하는지 확인하고, 없으면 생성
    /// </summary>
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// 파일이 존재하는지 확인
    /// </summary>
    public static void EnsureFileExists(string filePath, string description)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"{description} file not found: {filePath}");
        }
    }

    /// <summary>
    /// 디렉토리가 존재하는지 확인
    /// </summary>
    public static void EnsureDirectoryExists(string directoryPath, string description)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException(
                $"{description} directory not found: {directoryPath}");
        }
    }

    /// <summary>
    /// 파일이 비어있지 않은지 확인
    /// </summary>
    public static void EnsureFileNotEmpty(string filePath, string description)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            throw new ArgumentException($"{description} file is empty: {filePath}");
        }
    }

    /// <summary>
    /// 이미지 분류 디렉토리 구조 검증
    /// </summary>
    public static void ValidateImageClassificationDirectory(string directoryPath, Action<string> logWarning)
    {
        var normalizedPath = NormalizePath(directoryPath);
        var subDirectories = Directory.GetDirectories(normalizedPath);

        if (subDirectories.Length == 0)
        {
            throw new ArgumentException(
                $"No label subdirectories found in dataset directory: {normalizedPath}");
        }

        foreach (var subDir in subDirectories)
        {
            var normalizedSubDir = NormalizePath(subDir);
            var imageFiles = Directory.GetFiles(normalizedSubDir)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Select(NormalizePath)
                .ToList();

            if (imageFiles.Count == 0)
            {
                logWarning($"No image files found in label directory: {normalizedSubDir}");
            }
        }
    }

    /// <summary>
    /// 데이터셋 파일 확장자 검증
    /// </summary>
    public static void ValidateDataFileExtension(string filePath, Action<string, object[]> logWarning)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !DataExtensions.Contains(extension))
        {
            logWarning(
                "Dataset file has unexpected extension: {Extension}. Expected: {ValidExtensions}",
                new object[] { extension, string.Join(", ", DataExtensions) });
        }
    }

    /// <summary>
    /// 데이터셋 파일 전체 검증 (존재여부, 확장자, 크기)
    /// </summary>
    public static void ValidateDatasetFile(string filePath, string description, Action<string, object[]> logWarning)
    {
        var normalizedPath = NormalizePath(filePath);
        EnsureFileExists(normalizedPath, description);
        ValidateDataFileExtension(normalizedPath, logWarning);
        EnsureFileNotEmpty(normalizedPath, description);
    }
}