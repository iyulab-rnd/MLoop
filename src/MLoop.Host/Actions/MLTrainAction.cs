using MLoop.Models;
using MLoop.Services;
using MLoop.Utils;

namespace MLoop.Actions
{
    public class MLTrainAction
    {
        public required TrainOptions Options { get; set; }
        public string? DataPath { get; internal set; }
        public string? TestPath { get; internal set; }
    }

    public class MLTrainActionResult
    {
        public TrainStatus Status { get; internal set; }
    }

    public class MLTrainActionExecutor
    {
        private readonly MLTrainService service;

        public MLTrainActionExecutor(MLTrainService service)
        {
            this.service = service;
        }

        public async Task<MLTrainActionResult> ExecuteAsync(MLTrainAction action, CancellationToken token)
        {
            var dataPath = action.DataPath ?? "./data.csv";
            var testPath = action.TestPath;

            var res = await service.TrainRequestAsync(action.Options, dataPath, testPath);

            return new MLTrainActionResult
            {
                Status = res.Status
            };
        }
    }
}