using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions defaultOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = new FlexibleNamingPolicy(),
            AllowTrailingCommas = true,
            Converters =
            {
                new JsonStringEnumConverter(new FlexibleNamingPolicy()),
                new FlexibleNamingJsonConverter()
            }
        };

        public static IList<JsonConverter> Converters => defaultOptions.Converters;

        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, defaultOptions);
        }

        public static string? Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, defaultOptions);
        }
    }
}
