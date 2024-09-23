using MLoop.Actions;

namespace MLoop.Models;

public class MLModel
{
    public required string Name { get; set; }
    public TrainState? TrainState { get; set; }
    public TrainResult? TrainResult { get; set; }
}
