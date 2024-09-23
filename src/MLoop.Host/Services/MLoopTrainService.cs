using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Actions;
using MLoop.Internal;
using MLoop.SystemText.Json;
using MLoop.Utils;
using System.Collections.Concurrent;

namespace MLoop.Services;

public class MLoopTrainService
{
    private readonly ILogger<MLoopTrainService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MLoopOptions _options;
    private FileSystemWatcher? _fileWatcher;
    private readonly ConcurrentDictionary<string, DateTime> _lastEventTimes;
    private readonly TimeSpan _eventDebounceInterval = TimeSpan.FromSeconds(1);
    private const string invokeFileName = "action.json";
    private const string skipFileName = "result.json";

    public MLoopTrainService(ILogger<MLoopTrainService> logger, IServiceProvider serviceProvider, IOptions<MLoopOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _lastEventTimes = new ConcurrentDictionary<string, DateTime>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MLoopTrainService is starting.");
        if (_options.Path == null)
        {
            _logger.LogError("Path is not set in MLoopOptions");
            return Task.CompletedTask;
        }

        // 3초 대기 후에 파일 감시 시작
        Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ContinueWith(_ =>
        {
            var files = Directory.GetFiles(_options.Path, invokeFileName, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                ProcessFile(file, true); // 초기 파일 검사 시에는 디바운싱을 건너뜀
            }

            _fileWatcher = new FileSystemWatcher(_options.Path)
            {
                Filter = invokeFileName,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            _fileWatcher.Changed += OnChanged;
            _fileWatcher.Created += OnChanged;
            _fileWatcher.Renamed += OnChanged;
            _fileWatcher.EnableRaisingEvents = true;
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MLoopTrainService is stopping.");
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
        }
        return Task.CompletedTask;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        ProcessFile(e.FullPath, false);
    }

    private async void ProcessFile(string filePath, bool isInitialScan)
    {
        if (!isInitialScan)
        {
            var now = DateTime.UtcNow;
            var lastEventTime = _lastEventTimes.GetOrAdd(filePath, now);

            if (now - lastEventTime < _eventDebounceInterval)
            {
                _logger.LogInformation("Ignored duplicate event for {filePath}", filePath);
                return;
            }

            _lastEventTimes[filePath] = now;
        }

        var skipFilePath = Path.Combine(Path.GetDirectoryName(filePath)!, skipFileName);
        if (File.Exists(skipFilePath))
        {
            _logger.LogInformation("Skipped processing {filePath} because {skipFileName} exists.", filePath, skipFileName);
            return;
        }

        var baseDir = Path.GetDirectoryName(filePath)!;

        MLTrainAction? action;
        try
        {
            var json = await RetryFile.ReadAllTextAsync(filePath);
            if (string.IsNullOrEmpty(json)) return;

            action = SystemText.Json.JsonHelper.Deserialize<MLTrainAction>(json);
            if (action == null)
            {
                throw new InvalidDataException($"Failed to deserialize action from {filePath}");
            }

            if (action.Options == null)
            {
                await BuildDefaultActionAsync(baseDir);
                return;
            }

            if (action.DataPath != null && IsAbsolutePath(action.DataPath) == false)
            {
                action.DataPath = Path.Combine(baseDir, action.DataPath);
            }

            if (action.TestPath != null && IsAbsolutePath(action.TestPath) == false)
            {
                action.TestPath = Path.Combine(baseDir, action.TestPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception occurred while processing {filePath}", filePath);
            return;
        }

        var executor = _serviceProvider.GetRequiredService<MLTrainActionExecutor>();
        await executor.ExecuteAsync(action);

        _logger.LogInformation("Enqueued action from {filePath}", filePath);
    }

    private async Task BuildDefaultActionAsync(string modelPath)
    {
        var action = await MLoopFactory.BuildDefaultActionAsync(modelPath, null);

        var actionFilePath = Path.Combine(modelPath, "action.json");
        var json = JsonHelper.Serialize(action);
        await File.WriteAllTextAsync(actionFilePath, json);

        await Task.Delay(TimeSpan.FromSeconds(2));

        ProcessFile(actionFilePath, true); // 디바운싱 건너뛰고 처리
    }

    private static bool IsAbsolutePath(string path)
    {
        var uri = new Uri(path, UriKind.RelativeOrAbsolute);
        return uri.IsAbsoluteUri;
    }
}