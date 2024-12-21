using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Services;
using MLoop.Storages;
using MLoop.Storages.Configuration;
using MLoop.Worker;
using MLoop.Worker.Configuration;
using MLoop.Worker.Pipeline;
using MLoop.Worker.Steps;
using MLoop.Worker.Steps.Registry;
using MLoop.Worker.Tasks.MLNetPredictTask;
using MLoop.Worker.Tasks.MLNetTrainTask;

var builder = Host.CreateApplicationBuilder(args);

// 설정 구성
builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));

// Worker 설정 구성
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<WorkerSettings>>().Value;
    if (string.IsNullOrEmpty(settings.WorkerId))
    {
        settings.WorkerId = $"worker_{Environment.MachineName}_{Guid.NewGuid():N}";
    }
    return settings;
});

// Storage 서비스 등록
builder.Services.AddSingleton<IFileStorage>(sp =>
{
    var basePath = builder.Configuration["Storage:BasePath"]
        ?? throw new InvalidOperationException("Storage:BasePath configuration is required");
    return new LocalFileStorage(basePath);
});

// MLoop 공통 서비스 등록
builder.Services.AddSingleton<JobManager>();
builder.Services.AddSingleton<ScenarioManager>();

// Step Runner 등록
builder.Services.AddSingleton<IStepRunner, MLNetTrainRunner>();
builder.Services.AddSingleton<IStepRunner, MLNetPredictRunner>();
builder.Services.AddSingleton<StepRegistry>();

// MLNet 프로세서 등록
builder.Services.AddSingleton(sp =>
    new MLNetTrainProcessor(
        sp.GetRequiredService<ILogger<MLNetTrainProcessor>>(),
        builder.Configuration["MLNet:TrainPath"]));

builder.Services.AddSingleton(sp =>
    new MLNetPredictProcessor(
        sp.GetRequiredService<ILogger<MLNetPredictProcessor>>(),
        builder.Configuration["MLNet:PredictPath"]));

// Pipeline 서비스 등록
builder.Services.AddSingleton<PipelineExecutor>();

// Worker 서비스 등록
builder.Services.AddHostedService<WorkerBackgroundService>();

// 로깅 구성
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

try
{
    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Critical error starting worker: {ex}");
    throw;
}