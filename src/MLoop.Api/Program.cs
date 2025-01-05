using Microsoft.AspNetCore.Http.Features;
using MLoop.Api.Services;
using MLoop.Api;
using MLoop.Services;
using MLoop.Api.Infrastructure.OData;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Diagnostics;
using MLoop;
using MLoop.Api.InputFormatters;

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
    })
    .AddJsonOptions(options => JsonHelper.ApplyTo(options.JsonSerializerOptions))
    .AddOData(options => options
        .AddRouteComponents("odata", EdmModelBuilder.GetEdmModel())
        .Select()
        .Filter()
        .OrderBy()
        .SetMaxTop(100)
        .Count()
        .Expand()
    );

// 나머지 서비스 설정
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddStorage(builder.Configuration);
builder.Services.AddSingleton<ScenarioManager>();
builder.Services.AddSingleton<JobManager>();
builder.Services.AddSingleton<ScenarioService>();
builder.Services.AddSingleton<JobService>();
builder.Services.AddSingleton<ModelService>();

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
app.UseStaticFiles();

// SPA fallback route - API가 아닌 모든 요청을 index.html로 리다이렉트
app.MapFallbackToFile("index.html");

app.Run();