using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.ObjectFactories;

namespace MLoop;

public static class YamlHelper
{
    private static readonly IDeserializer DefaultDeserializer;
    private static readonly ISerializer DefaultSerializer;

    static YamlHelper()
    {
        DefaultDeserializer = CreateDeserializerBuilder().Build();
        DefaultSerializer = CreateSerializerBuilder().Build();
    }

    public static DeserializerBuilder CreateDeserializerBuilder()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner),
                s => s.InsteadOf<ObjectNodeDeserializer>())
            .IgnoreUnmatchedProperties();
    }

    public static SerializerBuilder CreateSerializerBuilder()
    {
        return new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull);
    }

    public static T? Deserialize<T>(string yaml)
    {
        return DefaultDeserializer.Deserialize<T>(yaml);
    }

    public static T? Deserialize<T>(TextReader reader)
    {
        return DefaultDeserializer.Deserialize<T>(reader);
    }

    public static object? Deserialize(string yaml, Type type)
    {
        return DefaultDeserializer.Deserialize(yaml, type);
    }

    public static object? Deserialize(TextReader reader, Type type)
    {
        return DefaultDeserializer.Deserialize(reader, type);
    }

    public static string Serialize<T>(T value)
    {
        return DefaultSerializer.Serialize(value);
    }

    public static void Serialize<T>(TextWriter writer, T value)
    {
        DefaultSerializer.Serialize(writer, value);
    }
}

public class ValidatingNodeDeserializer : INodeDeserializer
{
    private readonly INodeDeserializer _nodeDeserializer;

    public ValidatingNodeDeserializer(INodeDeserializer nodeDeserializer)
    {
        _nodeDeserializer = nodeDeserializer;
    }

    public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
    {
        if (_nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer))
        {
            if (expectedType == typeof(Dictionary<string, object>) ||
                expectedType == typeof(IDictionary<string, object>))
            {
                if (value is IDictionary<object, object> dict)
                {
                    value = ConvertToDictionary(dict);
                }
                else if (value is Dictionary<string, object> strDict)
                {
                    value = ConvertDictionaryValues(strDict);
                }
            }
            return true;
        }
        return false;
    }

    private Dictionary<string, object> ConvertDictionaryValues(Dictionary<string, object> dictionary)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, val) in dictionary)
        {
            result[key] = ConvertValue(val);
        }
        return result;
    }

    private Dictionary<string, object> ConvertToDictionary(IDictionary<object, object> dictionary)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in dictionary)
        {
            if (kvp.Key is string key)
            {
                var convertedValue = ConvertValue(kvp.Value);
                result[key] = convertedValue;
            }
        }
        return result;
    }

    private object ConvertValue(object? value)
    {
        return value switch
        {
            IDictionary<object, object> dict => ConvertToDictionary(dict),
            Dictionary<string, object> strDict => ConvertDictionaryValues(strDict),
            IList<object> list => list.Select(ConvertValue).ToList(),
            null => string.Empty,
            _ => value
        };
    }
}