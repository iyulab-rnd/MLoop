using Microsoft.Extensions.Configuration;
using MLoop.Models;
using System;

namespace MLoop.Services
{
    public class MLTrainService
    {
        private readonly MLTrainPool pool;
        private readonly Random random = new();

        public MLTrainService(IConfiguration configuration)
        {
            var threads = configuration.Resolve<int>("MLoop:Threads");
            this.pool = new MLTrainPool(threads);
        }

        public async Task<TrainResponse> TrainRequestAsync(TrainOptions options, string dataPath, string? testPath)
        {
            await Task.Delay(random.Next(10, 500)); // 임시 딜레이

            var dir = Path.GetDirectoryName(dataPath)!;
            var key = await TrainStateHandler.ResolveKey(dir); // 기존 키를 가져오거나 새로 생성 (Status: Pending)
            return pool.Train(new TrainRequest(key, options, dataPath, testPath));
        }

        public IEnumerable<string> GetWorkingModels()
        {
            return pool.GetWorkingModels();
        }
    }
}