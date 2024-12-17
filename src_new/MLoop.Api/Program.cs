using Microsoft.AspNetCore.Http.Features;
using MLoop.Api.Services;
using MLoop.Api;
using MLoop.Services;
using MLoop.Api.Infrastructure.OData;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

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

// 나머지 서비스 설정
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddStorage(builder.Configuration);
builder.Services.AddSingleton<ScenarioManager>();
builder.Services.AddSingleton<JobManager>();
builder.Services.AddSingleton<IMlnetRunner, MlnetCliRunner>();
builder.Services.AddSingleton<ScenarioService>();
builder.Services.AddSingleton<JobService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
