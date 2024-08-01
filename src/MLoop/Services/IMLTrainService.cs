using MLoop.Models;

namespace MLoop.Services
{
    public interface IMLTrainService
    {
        Task<TrainResponse> TrainRequestAsync(TrainOptions options, string dataPath, string? testPath);
    }
}