using MLoop.Worker.Tasks.CmdTask;

namespace MLoop.Worker.Tasks.MLNetPredictTask;

// Predict-specific request
public record MLNetPredictRequest : CmdProcessRequest
{
    /// <summary>
    /// Path to the trained model
    /// </summary>
    public required string ModelPath { get; init; }

    /// <summary>
    /// Path to input data for prediction
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Path where prediction results will be saved
    /// </summary>
    public required string OutputPath { get; init; }
}