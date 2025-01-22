using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Services;
using MLoop.Storages;
using MLoop.Storages.Configuration;
using MLoop.Worker.Configuration;
using MLoop.Worker.Pipeline;
using MLoop.Worker.Services;
using MLoop.Worker.Tasks.MLNet.Predict;
using MLoop.Worker.Tasks.MLNet.Train;
using MLoop.Worker.Tasks.MLNet.StepRunners;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
#if DEBUG
    EnvironmentName = "Development"
#else
    EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                     ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                     ?? "Production"
#endif
});

// Add configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure services
ConfigureServices(builder.Environment, builder.Services, builder.Configuration);

try
{
    // Set shutdown timeout
    var shutdownTimeout = builder.Configuration.GetValue("ShutdownTimeout", 30);
    Environment.SetEnvironmentVariable("DOTNET_SHUTDOWNTIMEOUTSECONDS", shutdownTimeout.ToString());

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Critical error starting worker: {ex}");
    throw;
}

static void ConfigureServices(IHostEnvironment env, IServiceCollection services, IConfiguration configuration)
{
    // Configuration
    services.Configure<WorkerSettings>(configuration.GetSection("Worker"));
    services.Configure<StorageSettings>(configuration.GetSection("Storage"));

    // Worker settings
    services.AddSingleton(sp =>
    {
        var settings = sp.GetRequiredService<IOptions<WorkerSettings>>().Value;
        if (string.IsNullOrEmpty(settings.WorkerId))
        {
            settings.WorkerId = $"worker_{Environment.MachineName}_{Guid.NewGuid():N}";
        }
        return settings;
    });

    // Storage
    services.AddSingleton<IFileStorage>(sp =>
    {
        var basePath = configuration["Storage:BasePath"]
            ?? throw new InvalidOperationException("Storage:BasePath configuration is required");
        return new LocalFileStorage(basePath);
    });

    // Core services
    services.AddSingleton<JobManager>();
    services.AddSingleton<ScenarioManager>();
    services.AddSingleton<WorkerRegistry>();

    // Pipeline
    services.AddSingleton<IPipeline, PipelineExecutor>();

    // Step runners
    services.AddSingleton<IStepRunner, MLNetTrainStepRunner>();
    services.AddSingleton<IStepRunner, MLNetPredictStepRunner>();
    services.AddSingleton<StepRegistry>();

    // BestModelManager 등록
    services.AddSingleton<BestModelManager>();
    
    // MLNet processors
    services.AddSingleton(sp =>
        new MLNetTrainProcessor(
            sp.GetRequiredService<ILogger<MLNetTrainProcessor>>(),
            configuration["MLNet:TrainPath"]));

    services.AddSingleton(sp =>
        new MLNetPredictProcessor(
            sp.GetRequiredService<ILogger<MLNetPredictProcessor>>(),
            configuration["MLNet:PredictPath"]));

    // Step runners
    services.AddStepRunners();

    // Worker services
    services.AddSingleton<JobProcessor>();
    services.AddHostedService<WorkerService>();

    // Logging
    services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();

        if (env.IsDevelopment())
        {
            logging.AddDebug();
        }

        logging.Configure(options =>
        {
            options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                            | ActivityTrackingOptions.TraceId
                                            | ActivityTrackingOptions.ParentId;
        });
    });
}