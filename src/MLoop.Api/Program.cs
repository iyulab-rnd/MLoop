using Microsoft.AspNetCore.Http.Features;
using MLoop.Api;
using MLoop.Services;
using MLoop.Api.Infrastructure.OData;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Diagnostics;
using MLoop.Api.InputFormatters;
using Microsoft.AspNetCore.StaticFiles;
using System.Text.Json;

var options = new WebApplicationOptions()
{
#if DEBUG
    WebRootPath = @"D:\data\MLoop\src\mloop-frontend\dist"
#else
    WebRootPath = "/var/data/wwwroot"
#endif
};
var builder = WebApplication.CreateBuilder(options);

// Add services to the container.
builder.Configuration.AddEnvironmentVariables();

var maxFileSize = 200 * 1024 * 1024; // 200MB
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxFileSize;
});

// Multipart Body Limit 및 MIME 타입 설정
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxFileSize;
    options.ValueLengthLimit = maxFileSize;
    options.MultipartHeadersLengthLimit = maxFileSize;
});

// MIME 타입 설정 추가
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true; // 필요한 경우 동기 I/O 허용
});

//OData를 AddControllers 체인 안에서 바로 설정
builder.Services
    .AddControllers(options =>
    {
        options.EnableEndpointRouting = false;
        options.InputFormatters.Add(new YamlInputFormatter());
        options.InputFormatters.Add(new TsvInputFormatter());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        JsonHelper.ApplyTo(options.JsonSerializerOptions);
    })
    .AddOData(options => options
        .AddRouteComponents("odata", EdmModelBuilder.GetEdmModel())
        .Select()
        .Filter()
        .OrderBy()
        .SetMaxTop(100)
        .Count()
        .Expand()
    );

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddStorage(builder.Configuration);

// Core services
builder.Services.AddSingleton<ScenarioManager>();
builder.Services.AddSingleton<JobManager>();
builder.Services.AddSingleton<WorkflowManager>();

// Add Handlers
builder.Services.AddScoped<JobHandler>();
builder.Services.AddScoped<WorkflowHandler>();
builder.Services.AddScoped<ScenarioHandler>();
builder.Services.AddScoped<ModelHandler>();

// Services
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<ScenarioService>();
builder.Services.AddScoped<ModelService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp => {
    errorApp.Run(async context => {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var response = new { error = exception?.Message };
        await context.Response.WriteAsJsonAsync(response);
    });
});

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.UseDefaultFiles();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".yaml"] = "text/yaml";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

// SPA fallback route - API가 아닌 모든 요청을 index.html로 리다이렉트
app.MapFallbackToFile("index.html");

app.Run();