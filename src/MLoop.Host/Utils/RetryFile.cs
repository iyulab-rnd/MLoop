namespace MLoop.Utils;

internal static class RetryFile
{
    /// <summary>
    /// 지정된 파일 경로에서 모든 텍스트를 비동기적으로 읽습니다.
    /// 일시적인 오류가 발생할 경우 최대 3번까지 재시도합니다.
    /// </summary>
    /// <param name="filePath">읽을 파일의 경로입니다.</param>
    /// <returns>파일의 내용이 문자열로 반환되거나, 실패 시 null을 반환합니다.</returns>
    internal static async Task<string?> ReadAllTextAsync(string filePath)
    {
        const int maxRetries = 3; // 최대 재시도 횟수
        const int delayMilliseconds = 500; // 초기 지연 시간 (밀리초)

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // 파일을 비동기적으로 읽습니다.
                return await File.ReadAllTextAsync(filePath);
            }
            catch (IOException) when (attempt < maxRetries)
            {
                // 지연 시간을 증가시켜 재시도합니다 (지수 백오프).
                await Task.Delay(delayMilliseconds * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < maxRetries)
            {
                await Task.Delay(delayMilliseconds * attempt);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // 모든 재시도가 실패한 경우 null을 반환합니다.
        Console.WriteLine($"Failed to read the file after {maxRetries} attempts.");
        return null;
    }
}
