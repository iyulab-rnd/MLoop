namespace MLoop.Models
{
    public record TrainRequest(string Key, MLScenarioTypes Type, TrainOptions Options, string DataPath, string? TestPath)
    {
        public override string ToString()
        {
            return $"{Key}|{Type}|{DataPath}";
        }
    }
}
