using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;

namespace MLoop.Api.InputFormatters;

public class YamlInputFormatter : InputFormatter
{
    public YamlInputFormatter()
    {
        SupportedMediaTypes.Add("text/yaml");
        SupportedMediaTypes.Add("application/x-yaml");
    }

    public override bool CanRead(InputFormatterContext context)
    {
        return true;
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body);

        // ReadToEnd()로 원본 텍스트 그대로 유지
        var yaml = await reader.ReadToEndAsync();

        // YAML 타입인 경우 문자열 그대로 반환
        if (context.ModelType == typeof(string))
        {
            return await InputFormatterResult.SuccessAsync(yaml);
        }

        // 다른 타입인 경우 YamlHelper를 통해 역직렬화
        var result = YamlHelper.Deserialize(yaml, context.ModelType);
        return await InputFormatterResult.SuccessAsync(result);
    }
}