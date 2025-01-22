using System.Text.Json;

namespace MLoop.Extensions;

public static class JsonElementExtensions
{
    public static IDictionary<string, object> ToTypedDictionary(
        this JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("JsonElement must be an object");
        }

        return element.EnumerateObject()
            .ToDictionary(
                prop => prop.Name,
                prop => ConvertJsonElement(prop.Value));
    }

    private static object ConvertJsonElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var longVal)
                ? longVal
                : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray()
                .Select(e => ConvertJsonElement(e))
                .ToArray(),
            JsonValueKind.Object => element.ToTypedDictionary(),
            _ => element.ToString()
        };
}
