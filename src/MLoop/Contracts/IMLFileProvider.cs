using System.Threading.Tasks;

namespace MLoop.Contracts
{
    public interface IMLFileProvider
    {
        Task<string> ReadFileAsync(string path, CancellationToken token);
        Task WriteFileAsync(string path, string content, CancellationToken token);
    }
}
