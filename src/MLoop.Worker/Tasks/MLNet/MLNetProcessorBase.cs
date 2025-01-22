using Microsoft.Extensions.Logging;
using MLoop.Models.Jobs;
using MLoop.Worker.Tasks.MLNet.Predict;
using MLoop.Worker.Tasks.MLNet.Train;
using System.Diagnostics;
using System.Text;

namespace MLoop.Worker.Tasks.MLNet;

public abstract class MLNetProcessorBase<T>
    where T : MLNetProcessResult
{
    protected readonly ILogger _logger;
    protected readonly string _cliPath;
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromHours(8);

    protected MLNetProcessorBase(ILogger logger, string cliPath)
    {
        _logger = logger;
        _cliPath = cliPath;
    }

    protected Process CreateProcess(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _cliPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_cliPath)
            }
        };

        _logger.LogInformation("Process working directory: {WorkingDirectory}",
            process.StartInfo.WorkingDirectory);
        return process;
    }

    protected async Task<MLNetProcessResult> RunProcessAsync(
        MLNetProcessRequest request,
        string arguments,
        CancellationToken cancellationToken)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var lastOutputTime = DateTime.UtcNow;
        var mlNetError = new MLNetError(); // 에러 정보를 저장하는 클래스

        using var process = CreateProcess(arguments);
        var startTime = DateTime.UtcNow;

        process.OutputDataReceived += async (_, e) =>
        {
            if (e.Data != null)
            {
                lastOutputTime = DateTime.UtcNow;
                outputBuilder.AppendLine(e.Data);
                _logger.LogInformation("[MLNet Output] {Output}", e.Data);
                await request.Context.LogAsync($"[MLNet Output] {e.Data}");

                // MLNet 에러 감지
                if (e.Data.Contains("Exception:") || e.Data.Contains("Error:"))
                {
                    ParseMLNetError(e.Data, mlNetError);
                }
            }
        };

        process.ErrorDataReceived += async (_, e) =>
        {
            if (e.Data != null)
            {
                lastOutputTime = DateTime.UtcNow;
                errorBuilder.AppendLine(e.Data);
                _logger.LogWarning("[MLNet Error] {Error}", e.Data);
                await request.Context.LogAsync($"[MLNet Error] {e.Data}");
            }
        };

        _logger.LogInformation("Starting process: {Command} {Arguments}",
            _cliPath, arguments);

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
            _logger.LogError(message);

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    _logger.LogWarning("Killed process tree");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error killing process");
            }

            throw new JobProcessException(JobFailureType.Timeout, message);
        }

        var result =  BuildResult(process, outputBuilder, errorBuilder, startTime, mlNetError);
        if (!result.Success)
        {
            var failureType = GetJobFailureType(mlNetError.ErrorType);
            var errorMessage = BuildErrorMessage(result, mlNetError);
            throw new JobProcessException(failureType, errorMessage);
        }
        return result;
    }

    protected virtual T BuildResult(Process process, StringBuilder outputBuilder, StringBuilder errorBuilder, DateTime startTime, MLNetError mlNetError)
    {
        var result = Activator.CreateInstance<T>();
        result.Success = process.ExitCode == 0 && !mlNetError.HasError;
        result.ExitCode = process.ExitCode;
        result.StandardOutput = outputBuilder.ToString();
        result.StandardError = errorBuilder.ToString();
        result.ProcessingTime = DateTime.UtcNow - startTime;
        result.ErrorType = mlNetError.ErrorType;
        result.ErrorMessage = mlNetError.Message;
        return result;
    }

    private static void ParseMLNetError(string line, MLNetError error)
    {
        if (error.HasError) return; // 이미 에러가 감지된 경우 추가 파싱 중단

        // 예외 타입 감지
        if (line.Contains("System.TimeoutException"))
        {
            error.ErrorType = MLNetErrorType.TimeoutException;
        }
        else if (line.Contains("System.OutOfMemoryException"))
        {
            error.ErrorType = MLNetErrorType.OutOfMemoryException;
        }
        else if (line.Contains("System.InvalidOperationException"))
        {
            error.ErrorType = MLNetErrorType.InvalidOperationException;
        }
        else if (line.Contains("System.ArgumentException"))
        {
            error.ErrorType = MLNetErrorType.ArgumentException;
        }
        else if (line.Contains("Training time finished without completing"))
        {
            error.ErrorType = MLNetErrorType.TrainingError;
        }
        else if (line.Contains("Invalid data format") || line.Contains("Data processing error"))
        {
            error.ErrorType = MLNetErrorType.DataProcessingError;
        }
        else if (line.Contains("Validation error") || line.Contains("Model validation failed"))
        {
            error.ErrorType = MLNetErrorType.ValidationError;
        }
        else if (line.Contains("Exception:") || line.Contains("Error:"))
        {
            error.ErrorType = MLNetErrorType.UnknownError;
        }

        // 에러 메시지 저장
        if (error.HasError && string.IsNullOrEmpty(error.Message))
        {
            error.Message = ExtractErrorMessage(line);
        }

        // 스택 트레이스 저장
        if (line.Contains("   at "))
        {
            error.StackTrace += line + Environment.NewLine;
        }
    }

    private static string ExtractErrorMessage(string line)
    {
        // Exception: message 형태에서 메시지 부분만 추출
        var colonIndex = line.IndexOf(':');
        if (colonIndex >= 0 && colonIndex + 1 < line.Length)
        {
            return line[(colonIndex + 1)..].Trim();
        }
        return line.Trim();
    }

    private static JobFailureType GetJobFailureType(MLNetErrorType errorType)
    {
        return errorType switch
        {
            MLNetErrorType.TimeoutException => JobFailureType.Timeout,
            MLNetErrorType.OutOfMemoryException => JobFailureType.ResourceExhausted,
            MLNetErrorType.TrainingError => JobFailureType.TrainingError,
            MLNetErrorType.DataProcessingError => JobFailureType.DataProcessingError,
            MLNetErrorType.ValidationError => JobFailureType.ValidationError,
            _ => JobFailureType.TrainingError
        };
    }

    private static string BuildErrorMessage(MLNetProcessResult result, MLNetError error)
    {
        var messageBuilder = new StringBuilder();

        if (error.HasError)
        {
            messageBuilder.AppendLine($"MLNet error: {error.ErrorType}");
            if (!string.IsNullOrEmpty(error.Message))
            {
                messageBuilder.AppendLine(error.Message);
            }
            if (!string.IsNullOrEmpty(error.StackTrace))
            {
                messageBuilder.AppendLine("Stack trace:");
                messageBuilder.AppendLine(error.StackTrace);
            }
        }
        else if (!string.IsNullOrEmpty(result.StandardError))
        {
            messageBuilder.AppendLine($"MLNet process failed with exit code {result.ExitCode}:");
            messageBuilder.AppendLine(result.StandardError);
        }
        else
        {
            messageBuilder.AppendLine($"MLNet process failed with exit code {result.ExitCode}");
        }

        return messageBuilder.ToString();
    }

    protected class MLNetError
    {
        public bool HasError => ErrorType != MLNetErrorType.None;
        public MLNetErrorType ErrorType { get; set; } = MLNetErrorType.None;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
    }
}
