namespace MLoop.Api.Exceptions;

public class WorkflowValidationException : Exception
{
    public WorkflowValidationException(string message) : base(message) { }
}