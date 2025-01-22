namespace MLoop.Worker.Tasks.MLNet.Predict;

public record MLNetPredictRequest : MLNetProcessRequest
{
    public required string ModelPath { get; init; }
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
}
