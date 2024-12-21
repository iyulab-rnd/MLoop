using MLoop.Models;
using MLoop.Services;

namespace MLoop.Actions
{
    public class MLTrainAction
    {
        public MLScenarioTypes Type { get; set; }
        public required TrainOptions Options { get; set; }
        public string? DataPath { get; set; }
        public string? TestPath { get; set; }
    }

    public class MLTrainActionResult
    {
        public required string Key { get; set; }
        public TrainStatus Status { get; internal set; }
    }

    public class MLTrainActionExecutor
    {
        private readonly MLTrainService service;

        public MLTrainActionExecutor(MLTrainService service)
        {
            this.service = service;
        }

        public async Task<MLTrainActionResult> ExecuteAsync(MLTrainAction action)
        {
            var dataPath = action.DataPath ?? throw new Exception($"Required DataPath");
            var testPath = action.TestPath;

            var res = await service.TrainRequestAsync(
                action.Type,
                action.Options, 
                dataPath, 
                testPath);

            return new MLTrainActionResult
            {
                Key = res.Key,
                Status = res.Status
            };
        }
    }
}