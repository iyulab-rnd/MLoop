namespace MLoop.Helpers;

public static class FileHelper
{
    private static readonly SemaphoreSlim _writeLock = new(1);

    public static async Task SafeAppendAllTextAsync(
        string filePath,
        string content,
        CancellationToken cancellationToken,
        int maxRetries = 3)
    {
        var retryCount = 0;
        var backoff = TimeSpan.FromMilliseconds(100);

        while (true)
        {
            try
            {
                await _writeLock.WaitAsync(cancellationToken);
                try
                {
                    var dirPath = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }

                    // FileShare.Read를 사용하여 다른 프로세스의 읽기는 허용
                    using var fileStream = new FileStream(
                        filePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read,
                        4096,
                        FileOptions.Asynchronous | FileOptions.WriteThrough);
                    using var writer = new StreamWriter(fileStream);

                    await writer.WriteAsync(content);
                    await writer.FlushAsync();
                    return;
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (IOException) when (retryCount < maxRetries)
            {
                retryCount++;
                await Task.Delay(backoff, cancellationToken);
                backoff *= 2; // 지수 백오프
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}