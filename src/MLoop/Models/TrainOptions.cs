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
        public bool AllowQuote { get; set; } = true;
        public bool HasHeader { get; set; } = true;
        public string? LabelCol { get; set; }
        public string? IgnoreCols { get; set; }

        /// <summary>
        /// seconds
        /// </summary>
        public int? TrainTime { get; set; }
    }
}
