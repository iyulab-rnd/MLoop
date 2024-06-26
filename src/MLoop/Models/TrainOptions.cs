using System.Text.Json.Serialization;

namespace MLoop.Models
{
    public enum ModelScenarios
    {
        Classification,
        Regression,
        Forecasting,
        Recommendation,
        ImageClassification,
        ObjectDetection,
        TextClassification
    }

    public class TrainOptions
    {
        public ModelScenarios Scenario { get; set; }
        public bool AllowQuote { get; set; }
        public bool HasHeader { get; set; }
        public string? LabelCol { get; set; }
        public string? IgnoreCols { get; set; }
        public int? TrainTime { get; set; }
    }
}
