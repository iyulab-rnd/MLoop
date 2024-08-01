using MLoop.Models;
using MLoop.Services;

namespace MLoop.Actions
{
    public class MLTrainAction
    {
        public required TrainOptions Options { get; set; }
        public string? DataPath { get; set; }
        public string? TestPath { get; set; }
    }

    public class MLTrainActionResult
    {
        public TrainStatus Status { get; internal set; }
    }

    public class MLTrainActionExecutor
    {
        private readonly IMLTrainService service;

        public MLTrainActionExecutor(IMLTrainService service)
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