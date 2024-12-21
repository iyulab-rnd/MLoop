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

var builder = WebApplication.CreateBuilder(args);

// WebRootPath ȯ�溯�� ����
var webRootPath = Environment.GetEnvironmentVariable("WebRootPath");
if (!string.IsNullOrEmpty(webRootPath))
    builder.WebHost.UseWebRoot(webRootPath);

// Add services to the container.
builder.Configuration.AddEnvironmentVariables();

var maxFileSize = 200 * 1024 * 1024; // 200MB
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxFileSize;
});

// Multipart Body Limit �� MIME Ÿ�� ����
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxFileSize;
    options.ValueLengthLimit = maxFileSize;
    options.MultipartHeadersLengthLimit = maxFileSize;
});

// MIME Ÿ�� ���� �߰�
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true; // �ʿ��� ��� ���� I/O ���
});

//OData�� AddControllers ü�� �ȿ��� �ٷ� ����
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

// ������ ���� ����
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

app.Run();
