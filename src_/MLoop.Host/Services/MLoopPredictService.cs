using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Timers;
using System.Diagnostics;

namespace MLoop.Services;

public class MLoopPredictService
{
    private readonly ILogger<MLoopPredictService> _logger;
    private readonly MLoopOptions _options;
    private FileSystemWatcher? _fileWatcher;
    private readonly ConcurrentDictionary<string, System.Timers.Timer> _debouncers;
    private readonly MLoopApiService _api;
    private const string InputFilePattern = "input_*.csv";

    public MLoopPredictService(ILogger<MLoopPredictService> logger, IOptions<MLoopOptions> options, MLoopApiService apiService)
    {
        _logger = logger;
        _options = options.Value;
        _debouncers = new ConcurrentDictionary<string, System.Timers.Timer>();
        _api = apiService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MLoopPredictService is starting.");
        if (_options.Path == null)
        {
            _logger.LogError("Path is not set in MLoopOptions");
            return Task.CompletedTask;
        }
        _fileWatcher = new FileSystemWatcher(_options.Path)
        {
            Filter = InputFilePattern,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };
        _fileWatcher.Changed += OnChanged;
        _fileWatcher.Created += OnChanged;
        _fileWatcher.Renamed += OnChanged;
        _fileWatcher.EnableRaisingEvents = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MLoopPredictService is stopping.");
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
        }
        foreach (var debouncer in _debouncers.Values)
        {
            debouncer.Dispose();
        }
        return Task.CompletedTask;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileNameWithoutExtension(e.FullPath);
        if (!fileName.StartsWith("input_", StringComparison.OrdinalIgnoreCase)) return;
        if (fileName.EndsWith("-predicted", StringComparison.OrdinalIgnoreCase)) return;

        _logger.LogInformation("File change detected: {filePath}", e.FullPath);
        var timer = _debouncers.GetOrAdd(e.FullPath, _ => new System.Timers.Timer(5000) { AutoReset = false });
        timer.Stop(); // Stop the timer if it's already running
        timer.Elapsed -= OnTimerElapsed;
        timer.Elapsed += OnTimerElapsed;
        timer.Start();

        void OnTimerElapsed(object? sender, ElapsedEventArgs args)
        {
            _debouncers.TryRemove(e.FullPath, out _);
            Task.Run(async () => await ProcessFileAsync(e.FullPath));
        }
    }

    private async Task ProcessFileAsync(string inputPath)
    {
        if (!File.Exists(inputPath)) return;

        var directory = Path.GetDirectoryName(inputPath)!;
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var ext = Path.GetExtension(inputPath);
        var resultFileName = $"{fileName}-predicted{ext}";
        var resultFilePath = Path.Combine(directory, resultFileName);
        var logFilePath = Path.Combine(directory, $"{fileName}.log");

        if (File.Exists(resultFilePath)) return;

        _logger.LogInformation("Processing file: {filePath}", inputPath);

        var relativePath = Path.GetRelativePath(_options.Path, directory);
        var scenarioPath = relativePath.Split(Path.DirectorySeparatorChar).ElementAt(0);
        var scenario = await _api.GetMLScenarioAsync(scenarioPath)
            ?? throw new InvalidOperationException("cannot find Scenario");
        var model = await _api.GetPredictionModelAsync(scenario)
            ?? throw new InvalidOperationException("cannot find Model");
        var modelPath = Path.Combine(_options.Path, scenario.Name, model.Name, "Model");

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "mlnet-predict",
                    Arguments = $"{modelPath} {inputPath}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("mlnet-predict process failed. Error: {error}", error);
                throw new Exception($"mlnet-predict process failed with exit code {process.ExitCode}");
            }

            // Write the output to the result file
            await File.WriteAllTextAsync(logFilePath, output);
            _logger.LogInformation("Prediction completed. Result file: {resultFilePath}", resultFilePath);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.Message.Contains("The system cannot find the file specified"))
        {
            _logger.LogError(ex, "Failed to start mlnet-predict. It may not be installed.");
            _logger.LogInformation("To install mlnet-predict, run the following command:");
            _logger.LogInformation("dotnet tool install -g mlnet-predict");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing file: {filePath}", inputPath);
        }
    }
}