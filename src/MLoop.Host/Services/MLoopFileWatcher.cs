using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Actions;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MLoop.Services
{
    public class MLoopFileWatcher : BackgroundService
    {
        private readonly ILogger<MLoopFileWatcher> _logger;
        private readonly Channel<BuildModelAction> _channel;
        private readonly MLoopOptions _options;
        private FileSystemWatcher? _fileWatcher;
        private readonly ConcurrentDictionary<string, DateTime> _lastEventTimes;
        private readonly TimeSpan _eventDebounceInterval = TimeSpan.FromSeconds(1);
        private const string invokeFileName = "action.json";
        private const string skipFileName = "result.json";
        private const string stateFileName = "state.log";

        public MLoopFileWatcher(ILogger<MLoopFileWatcher> logger, Channel<BuildModelAction> channel, IOptions<MLoopOptions> options)
        {
            _logger = logger;
            _channel = channel;
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

            await Task.CompletedTask;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            ProcessFile(e.FullPath, false);
        }

        private void ProcessFile(string filePath, bool isInitialScan)
        {
            if (!isInitialScan)
            {
                var now = DateTime.UtcNow;
                var lastEventTime = _lastEventTimes.GetOrAdd(filePath, now);

                if (now - lastEventTime < _eventDebounceInterval)
                {
                    _logger.LogInformation($"Ignored duplicate event for {filePath}");
                    return;
                }

                _lastEventTimes[filePath] = now;
            }

            var skipFilePath = Path.Combine(Path.GetDirectoryName(filePath)!, skipFileName);
            if (File.Exists(skipFilePath))
            {
                _logger.LogInformation($"Skipped processing {filePath} because {skipFileName} exists.");
                return;
            }

            BuildModelAction? action;
            try
            {
                action = JsonHelper.Deserialize<BuildModelAction>(File.ReadAllText(filePath));
                if (action == null)
                {
                    throw new InvalidDataException($"Failed to deserialize action from {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Exception occurred while processing {filePath}");
                return;
            }

            _channel.Writer.TryWrite(action);
            _logger.LogInformation($"Enqueued action from {filePath}");
            LogState("PENDING");
        }

        private void LogState(string state)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}|{state}";
            File.AppendAllLines(Path.Combine(_options.Path, stateFileName), new[] { logEntry });
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
