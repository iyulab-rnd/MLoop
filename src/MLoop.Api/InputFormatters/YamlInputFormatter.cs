using Microsoft.AspNetCore.Mvc.Formatters;

namespace MLoop.Api.InputFormatters;

public class YamlInputFormatter : InputFormatter
{
    public YamlInputFormatter()
    {
        SupportedMediaTypes.Add("text/yaml");
        SupportedMediaTypes.Add("application/x-yaml");
        SupportedMediaTypes.Add("application/yaml");
    }

    public override bool CanRead(InputFormatterContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var contentType = context.HttpContext.Request.ContentType;
        return contentType?.Contains("yaml") ?? false;
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        try
        {
            using var reader = new StreamReader(context.HttpContext.Request.Body);
            var yaml = await reader.ReadToEndAsync();

            // string 타입이면 직접 반환
            if (context.ModelType == typeof(string))
            {
                return await InputFormatterResult.SuccessAsync(yaml);
            }

            var result = YamlHelper.Deserialize(yaml, context.ModelType);
            return await InputFormatterResult.SuccessAsync(result);
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<YamlInputFormatter>>();
            logger?.LogError(ex, "Error parsing YAML request");
            return await InputFormatterResult.FailureAsync();
        }
    }
}