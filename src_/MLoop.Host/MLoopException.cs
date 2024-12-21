namespace MLoop;

public class MLoopException : Exception
{
    public int StatusCode { get; set; } = 400;
    public object Details { get; set; }

    public MLoopException(string? message) : base(message)
    {
    }
}