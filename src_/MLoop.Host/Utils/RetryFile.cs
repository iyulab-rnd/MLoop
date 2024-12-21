namespace MLoop.Utils;

internal static class RetryFile
{
    private const int MaxRetries = 3;
    private const int DelayMilliseconds = 500;

    internal static async Task<string?> ReadAllTextAsync(string filePath)
    {
        return await RetryOperationAsync(() => File.ReadAllTextAsync(filePath));
    }

    internal static async Task WriteAllTextAsync(string filePath, string contents)
    {
        await RetryOperationAsync(() => File.WriteAllTextAsync(filePath, contents));
    }

    internal static async Task AppendAllTextAsync(string filePath, string contents)
    {
        await RetryOperationAsync(() => File.AppendAllTextAsync(filePath, contents));
    }

    internal static async Task<string?> ReadFirstLineAsync(string filePath)
    {
        return await RetryOperationAsync(async () =>
        {
            using var reader = new StreamReader(filePath);
            return await reader.ReadLineAsync();
        });
    }

    private static async Task<T?> RetryOperationAsync<T>(Func<Task<T>> operation)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (IOException) when (attempt < MaxRetries)
            {
                await Task.Delay(DelayMilliseconds * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < MaxRetries)
            {
                await Task.Delay(DelayMilliseconds * attempt);
            }
        }

        Console.WriteLine($"Failed to perform operation after {MaxRetries} attempts.");
        return default;
    }

    private static async Task RetryOperationAsync(Func<Task> operation)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (IOException) when (attempt < MaxRetries)
            {
                await Task.Delay(DelayMilliseconds * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < MaxRetries)
            {
                await Task.Delay(DelayMilliseconds * attempt);
            }
        }

        Console.WriteLine($"Failed to perform operation after {MaxRetries} attempts.");
    }
}