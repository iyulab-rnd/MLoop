using MLoop.Models;
using MLoop.Worker.Tasks.CmdTask;

namespace MLoop.Worker.Tasks.MLNetTrainTask;

public class MLNetTrainProcessResult : CmdProcessResult
{
    public MLNetTrainProcessResult()
    {
    }

    public MLModelMetrics? Metrics { get; private set; }
    public string? ModelPath { get; init; }

    public async Task ParseAsync(string modelPath)
    {
        Metrics = await MLNetTrainStaticMethods.GetMLModelMetricsAsync(modelPath);
    }
}