using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MLoop.Helpers;

public static class ArgumentHelper
{
    public static IDictionary<string, object> ParseConfigArguments(
        ILogger logger,
        object? configArgs)
    {
        if (configArgs == null)
        {
            logger.LogWarning("Configuration args is null");
            return new Dictionary<string, object>();
        }

        try
        {
            return configArgs switch
            {
                IDictionary<string, object> stringDict => stringDict,
                IDictionary<object, object> objDict => objDict.ToStringDictionary(),
                JsonElement jsonElement => jsonElement.ToTypedDictionary(),
                _ => new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse configuration arguments");
            throw new ArgumentException("Invalid configuration format", ex);
        }
    }

    public static string ResolveDataPath(
        string basePath,
        IDictionary<string, object> args,
        string key = "dataset")
    {
        if (args.TryGetValue(key, out var path))
        {
            return Path.GetFullPath(Path.Combine(
                basePath,
                path?.ToString() ?? string.Empty));
        }
        return basePath;
    }
}

