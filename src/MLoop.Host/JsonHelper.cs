using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MLoop
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions defaultOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static T? Deserialize<T>(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, defaultOptions);
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.WriteLine(e.Message);
                Debugger.Break();
                throw;
#else
                return default;
#endif
            }
        }
    }
}
