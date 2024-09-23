using System.Text.Json.Serialization;

namespace MLoop.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MLScenarioTypes
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
        public bool AllowQuote { get; set; } = true;
        public bool HasHeader { get; set; } = true;
        
        public string? LabelCol { get; set; }
        public string? IgnoreCols { get; set; }

        public string? ItemCol { get; set; } // Recommendation
        public string? RatingCol { get; set; } // Recommendation
        public string? UserCol { get; set; } // Recommendation

        /// <summary>
        /// seconds
        /// </summary>
        public int? TrainTime { get; set; }
    }
}
