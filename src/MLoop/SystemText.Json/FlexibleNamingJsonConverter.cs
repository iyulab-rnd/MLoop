using System.Text.Json.Serialization;
using System.Reflection;
using System.Text.Json;

namespace MLoop.SystemText.Json
{
    public class FlexibleNamingJsonConverter : JsonConverter<object>
    {
        private static readonly Type[] _knownTypes =
        {
            typeof(string), typeof(int), typeof(long), typeof(float),
            typeof(double), typeof(decimal), typeof(bool), typeof(DateTime),
            typeof(Guid)
        };

        public override bool CanConvert(Type typeToConvert)
        {
            return !_knownTypes.Contains(typeToConvert) && !typeToConvert.IsPrimitive && (!typeToConvert.IsGenericType || typeToConvert.GetGenericTypeDefinition() != typeof(Nullable<>));
        }

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var obj = Activator.CreateInstance(typeToConvert, true)!;
            var properties = typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return obj;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                var propertyName = reader.GetString()!;
                reader.Read();

                var property = properties.FirstOrDefault(p => FlexibleNamingPolicy.Camelize(p.Name).Equals(FlexibleNamingPolicy.Camelize(propertyName), StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    if (property.CanWrite != true) continue;

                    object? value = null;

                    try
                    {
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            value = null;
                        }
                        else if (property.PropertyType == typeof(string))
                        {
                            value = reader.TokenType switch
                            {
                                JsonTokenType.String => reader.GetString(),
                                JsonTokenType.Number => reader.GetDecimal().ToString(),
                                JsonTokenType.True => "true",
                                JsonTokenType.False => "false",
                                _ => throw new JsonException($"Unexpected token type {reader.TokenType} for property {propertyName}")
                            };
                        }
                        else if (reader.TokenType == JsonTokenType.Number)
                        {
                            if (property.PropertyType == typeof(int) || Nullable.GetUnderlyingType(property.PropertyType) == typeof(int))
                            {
                                value = reader.GetInt32();
                            }
                            else if (property.PropertyType == typeof(long) || Nullable.GetUnderlyingType(property.PropertyType) == typeof(long))
                            {
                                value = reader.GetInt64();
                            }
                            else if (property.PropertyType == typeof(float) || Nullable.GetUnderlyingType(property.PropertyType) == typeof(float))
                            {
                                value = reader.GetSingle();
                            }
                            else if (property.PropertyType == typeof(double) || Nullable.GetUnderlyingType(property.PropertyType) == typeof(double))
                            {
                                value = reader.GetDouble();
                            }
                            else if (property.PropertyType == typeof(decimal) || Nullable.GetUnderlyingType(property.PropertyType) == typeof(decimal))
                            {
                                value = reader.GetDecimal();
                            }
                            else
                            {
                                value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                            }
                        }
                        else if (property.PropertyType.IsEnum)
                        {
                            value = Enum.Parse(property.PropertyType, reader.GetString()!, true);
                        }
                        else
                        {
                            value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                        }

                        property.SetValue(obj, value);
                    }
                    catch (JsonException ex)
                    {
                        throw new JsonException($"Error deserializing property '{propertyName}' of type '{property.PropertyType}': {ex.Message}");
                    }
                }
            }

            throw new JsonException("Unable to read JSON.");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            var newOptions = new JsonSerializerOptions(options);
            var converters = newOptions.Converters.Where(c => c.GetType() != typeof(FlexibleNamingJsonConverter)).ToList();
            newOptions.Converters.Clear();
            foreach (var converter in converters)
            {
                newOptions.Converters.Add(converter);
            }

            JsonSerializer.Serialize(writer, value, newOptions);
        }
    }
}