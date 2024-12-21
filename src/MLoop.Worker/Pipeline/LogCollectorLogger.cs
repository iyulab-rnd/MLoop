using Microsoft.Extensions.Logging;
using System.Text;

namespace MLoop.Worker.Pipeline;

public class LogCollectorLogger : ILogger
{
    private readonly ILogger _innerLogger;
    private readonly StringBuilder _logCollector;

    public LogCollectorLogger(ILogger innerLogger, StringBuilder logCollector)
    {
        _innerLogger = innerLogger;
        _logCollector = logCollector;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _innerLogger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => _innerLogger.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // 내부 로거에 기록
        _innerLogger.Log(logLevel, eventId, state, exception, formatter);

        // 로그 수집기에 기록
        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var formattedMessage = $"[{timestamp}] [{logLevel}] {message}";

        if (exception != null)
        {
            formattedMessage += Environment.NewLine + exception;
        }

        _logCollector.AppendLine(formattedMessage);
    }
}