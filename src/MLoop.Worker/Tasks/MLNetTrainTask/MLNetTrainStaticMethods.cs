using MLoop.Models;

namespace MLoop.Worker.Tasks.MLNetTrainTask;

public static class MLNetTrainStaticMethods
{
    public static Task<MLModelMetrics> GetMLModelMetricsAsync(string modelPath)
    {
        return MLModelMetrics.ParseForMLNetAsync(modelPath);
    }

    internal static async Task SaveModelMetadataAsync(
        string modelId, string command, Dictionary<string, object> args,
        MLModelMetrics? metrics,
        string modelPath)
    {
        // 1. Save model metadata
        var model = new MLModel(
            modelId: modelId,
            mlType: command,
            command: command,
            arguments: args
        );

        if (metrics != null)
        {
            model.Metrics = metrics;
        }

        var modelMetadataPath = Path.Combine(modelPath, "metadata.json");
        var modelMetadataJson = JsonHelper.Serialize(model);
        await File.WriteAllTextAsync(modelMetadataPath, modelMetadataJson);

        // 2. Save metrics separately if available
        if (model.Metrics != null)
        {
            var metricsPath = Path.Combine(modelPath, "metrics.json");
            var metricsJson = JsonHelper.Serialize(model.Metrics);
            await File.WriteAllTextAsync(metricsPath, metricsJson);
        }
    }

    internal static async Task UpdateModelMetadataAsync(string modelPath, string modelId, string command)
    {
        var metric = await MLNetTrainStaticMethods.GetMLModelMetricsAsync(modelPath);

        await SaveModelMetadataAsync(modelId, command, [], metric, modelPath);
    }
}
