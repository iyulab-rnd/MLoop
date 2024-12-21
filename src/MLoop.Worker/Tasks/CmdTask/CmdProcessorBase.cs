using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace MLoop.Worker.Tasks.CmdTask;

public abstract class CmdProcessorBase<TCmdProcessResult>
    where TCmdProcessResult : CmdProcessResult, new()
{
    protected readonly ILogger Logger;
    protected readonly string CliPath;
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromHours(8);

    protected CmdProcessorBase(ILogger logger, string cliPath)
    {
        Logger = logger;
        CliPath = cliPath;
    }

    protected Process CreateProcess(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = CliPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(CliPath)
            }
        };

        Logger.LogInformation("Process working directory: {WorkingDirectory}",
            process.StartInfo.WorkingDirectory);
        return process;
    }

    protected async Task<TCmdProcessResult> RunProcessAsync(
        CmdProcessRequest request,
        string arguments,
        CancellationToken cancellationToken)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var lastOutputTime = DateTime.UtcNow;

        using var process = CreateProcess(arguments);
        var startTime = DateTime.UtcNow;

        process.OutputDataReceived += async (_, e) =>
        {
            if (e.Data != null)
            {
                lastOutputTime = DateTime.UtcNow;
                outputBuilder.AppendLine(e.Data);
                Logger.LogInformation("[MLNet Output] {Output}", e.Data);

                // 작업 로그에 실시간으로 추가
                await request.Context.LogAsync($"[MLNet Output] {e.Data}");
            }
        };

        process.ErrorDataReceived += async (_, e) =>
        {
            if (e.Data != null)
            {
                lastOutputTime = DateTime.UtcNow;
                errorBuilder.AppendLine(e.Data);
                Logger.LogWarning("[MLNet Error] {Error}", e.Data);

                // 에러 로그도 실시간으로 추가
                await request.Context.LogAsync($"[MLNet Output] {e.Data}");
            }
        };

        Logger.LogInformation("Starting process: {Command} {Arguments}",
            CliPath, arguments);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(request.Timeout ?? DefaultTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            var message = $"Process timed out after {DateTime.UtcNow - startTime}";
            Logger.LogError(message);

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    Logger.LogWarning("Killed process tree");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error killing process");
            }

            throw new TimeoutException(message);
        }

        return BuildCmdProcessResult(process, outputBuilder, errorBuilder, DateTime.UtcNow - startTime);
    }

    protected virtual TCmdProcessResult BuildCmdProcessResult(Process process, StringBuilder outputBuilder, StringBuilder errorBuilder, TimeSpan processingTime)
    {
        return new TCmdProcessResult
        {
            Success = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString(),
            ProcessingTime = processingTime
        };
    }
}