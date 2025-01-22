using MLoop.Models.Jobs;

namespace MLoop.Helpers;

public static class ExceptionHelper
{
    public static (JobFailureType failureType, string message) GetFailureDetails(Exception ex)
    {
        if (ex is JobProcessException jpe)
        {
            return (jpe.FailureType, ex.Message);
        }

        return ex switch
        {
            FileNotFoundException => (JobFailureType.FileNotFound, ex.Message),
            InvalidOperationException => (JobFailureType.ConfigurationError, ex.Message),
            OperationCanceledException => (JobFailureType.Timeout, "Operation was cancelled"),
            NotImplementedException => (JobFailureType.UnknownError, ex.Message),
            _ => (JobFailureType.ProcessError, ex.Message)
        };
    }
}
