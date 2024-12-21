using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MLoop.WebApp.Controllers;

public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger = logger;

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is MLoopException mLoopException)
        {
            var result = new ObjectResult(new
            {
                message = mLoopException.Message,
                details = mLoopException.Details
            })
            {
                StatusCode = mLoopException.StatusCode
            };

            context.Result = result;
            context.ExceptionHandled = true;
            return;
        }
    }
}