using System.Text.Json;
using System.Text.Json.Serialization;

namespace MLoop;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static JsonHelper()
    {
        DefaultOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, DefaultOptions);
    }

    public static string Serialize<T>(T value, JsonSerializerOptions? options)
    {
        return JsonSerializer.Serialize(value, options ?? DefaultOptions);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions? options)
    {
        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }

    public static T? Deserialize<T>(JsonElement element)
    {
        return JsonSerializer.Deserialize<T>(element.GetRawText(), DefaultOptions);
    }

    public static T? Deserialize<T>(JsonElement element, JsonSerializerOptions? options)
    {
        return JsonSerializer.Deserialize<T>(element.GetRawText(), options ?? DefaultOptions);
    }

    public static JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions(DefaultOptions);
    }

    public static void ApplyTo(JsonSerializerOptions target)
    {
        target.PropertyNameCaseInsensitive = DefaultOptions.PropertyNameCaseInsensitive;
        target.PropertyNamingPolicy = DefaultOptions.PropertyNamingPolicy;
        target.WriteIndented = DefaultOptions.WriteIndented;
        target.DefaultIgnoreCondition = DefaultOptions.DefaultIgnoreCondition;

        foreach (var converter in DefaultOptions.Converters)
        {
            target.Converters.Add(converter);
        }
    }
}