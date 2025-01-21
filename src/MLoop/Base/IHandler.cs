using Microsoft.Extensions.Logging;

namespace MLoop.Base;

public interface IHandler<TEntity> where TEntity : class, IScenarioEntity
{
    Task<TEntity> ProcessAsync(TEntity entity);
    Task ValidateAsync(TEntity entity);
}

public abstract class HandlerBase<TEntity> : IHandler<TEntity> where TEntity : class, IScenarioEntity
{
    protected readonly ILogger _logger;

    protected HandlerBase(ILogger logger)
    {
        _logger = logger;
    }

    public abstract Task<TEntity> ProcessAsync(TEntity entity);
    public abstract Task ValidateAsync(TEntity entity);

    protected virtual async Task LogAsync(string message)
    {
        _logger.LogInformation(message);
        await Task.CompletedTask;
    }
}