using MLoop.Contracts;

namespace MLoop.Services
{
    public class LocalFileProvider : IMLFileProvider
    {
        public async Task<string> ReadFileAsync(string path, CancellationToken token)
        {
            using var reader = new StreamReader(path);
            return await reader.ReadToEndAsync(token);
        }

        public async Task WriteFileAsync(string path, string content, CancellationToken token)
        {
            using var writer = new StreamWriter(path);
            await writer.WriteAsync(content);
        }
    }
}
