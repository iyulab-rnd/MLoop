namespace MLoop.Api.Models;

public class RequestValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }

    private RequestValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static RequestValidationResult Success() => new(true);
    public static RequestValidationResult Fail(string errorMessage) => new(false, errorMessage);
}