using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Actions;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MLoop.Services
{
    public class MLoopService : BackgroundService
    {
        private readonly ILogger<MLoopService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly MLoopOptions _options;
        private FileSystemWatcher? _fileWatcher;
        private readonly ConcurrentDictionary<string, DateTime> _lastEventTimes;
        private readonly TimeSpan _eventDebounceInterval = TimeSpan.FromSeconds(1);
        private const string invokeFileName = "action.json";
        private const string skipFileName = "result.json";

        public MLoopService(ILogger<MLoopService> logger, IServiceProvider serviceProvider, IOptions<MLoopOptions> options)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _lastEventTimes = new ConcurrentDictionary<string, DateTime>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MLoopFileWatcher is starting.");
            if (_options.Path == null)
            {
                _logger.LogError("Path is not set in MLoopOptions");
                return;
            }

            // 3초 대기 후에 파일 감시 시작
            await Task.Delay(TimeSpan.FromSeconds(3));

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
            _fileWatcher.EnableRaisingEvents = true;
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
                action = JsonHelper.Deserialize<MLTrainAction>(File.ReadAllText(filePath));
                if (action == null)
                {
                    throw new InvalidDataException($"Failed to deserialize action from {filePath}");
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
            await executor.ExecuteAsync(action, CancellationToken.None);

            _logger.LogInformation("Enqueued action from {filePath}", filePath);
        }

        private static bool IsAbsolutePath(string path)
        {
            var uri = new Uri(path, UriKind.RelativeOrAbsolute);
            return uri.IsAbsoluteUri;
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MLoopFileWatcher is stopping.");
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
            }
            return base.StopAsync(stoppingToken);
        }
    }
}
