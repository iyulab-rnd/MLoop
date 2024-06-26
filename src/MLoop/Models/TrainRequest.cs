namespace MLoop.Models
{
    public record TrainRequest(string Key, TrainOptions Options, string DataPath, string? TestPath)
    {
        public override string ToString()
        {
            return $"{Key}|{Options.Scenario}|{DataPath}";
        }
    }
}
