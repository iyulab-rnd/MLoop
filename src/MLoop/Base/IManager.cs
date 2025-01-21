using Microsoft.Extensions.Logging;

namespace MLoop.Base;

public interface IScenarioManager<TEntity> where TEntity : class, IScenarioEntity
{
    Task<TEntity?> LoadAsync(string scenarioId, string id);
    Task SaveAsync(string scenarioId, string id, TEntity entity);
    Task DeleteAsync(string scenarioId, string id);
    Task<bool> ExistsAsync(string scenarioId, string id);
}

public abstract class ScenarioManagerBase<TEntity> : IScenarioManager<TEntity> where TEntity : class, IScenarioEntity
{
    protected readonly IFileStorage _storage;
    protected readonly ILogger _logger;

    protected ScenarioManagerBase(IFileStorage storage, ILogger logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public abstract Task<TEntity?> LoadAsync(string scenarioId, string id);
    public abstract Task SaveAsync(string scenarioId, string id, TEntity entity);
    public abstract Task DeleteAsync(string scenarioId, string id);
    public abstract Task<bool> ExistsAsync(string scenarioId, string id);
}
