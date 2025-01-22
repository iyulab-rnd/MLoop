namespace MLoop.Extensions;

public static class DictionaryExtensions
{
    public static T GetValueOrDefault<T>(
        this IDictionary<string, object> dict,
        string key,
        T defaultValue = default)
    {
        if (dict.TryGetValue(key, out var value))
        {
            try
            {
                // IDictionary 타입인 경우 특별 처리
                if (typeof(T).IsGenericType &&
                    (typeof(T).GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                     typeof(T).GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    if (value is IDictionary<object, object> objDict)
                    {
                        return (T)(object)objDict.ToStringDictionary();
                    }
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public static IDictionary<string, object> ToStringDictionary(
        this IDictionary<object, object> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key?.ToString() ?? string.Empty,
            kvp => kvp.Value ?? string.Empty);
    }
}