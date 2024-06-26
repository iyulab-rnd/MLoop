using System;
using System.Text.Json;
using Humanizer;

namespace System.Text.Json
{
    internal class FlexibleNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return Camelize(name);
        }

        public static string Camelize(string name)
        {
            // 다양한 형식을 camelCase로 변환
            string camelized = name.Camelize();

            // KebabCase, SnakeCase, PascalCase 처리
            camelized = camelized.Replace("-", " ").Replace("_", " ").Pascalize().Camelize();

            return camelized;
        }

        public static string NormalizePropertyName(string name)
        {
            string[] variations =
            [
                name.Camelize(),
                name.Pascalize(),
                name.Kebaberize(),
                name.Underscore()
            ];

            foreach (var variation in variations)
            {
                if (string.Equals(name, variation, StringComparison.OrdinalIgnoreCase))
                {
                    return variation;
                }
            }

            return name; // 일치하는 변환이 없을 경우 원래 이름 반환
        }
    }
}
