using Microsoft.Extensions.Logging;

namespace MLoop.Base;

public interface IScenarioService<TEntity, TCreateRequest, TUpdateRequest>
    where TEntity : class, IScenarioEntity
    where TCreateRequest : class
    where TUpdateRequest : class
{
    Task<TEntity> CreateAsync(string scenarioId, TCreateRequest request);
    Task<TEntity?> GetAsync(string scenarioId, string id);
    Task<TEntity> UpdateAsync(string scenarioId, string id, TUpdateRequest request);
    Task DeleteAsync(string scenarioId, string id);
}

public abstract class ScenarioServiceBase<TEntity, TCreateRequest, TUpdateRequest>
    : IScenarioService<TEntity, TCreateRequest, TUpdateRequest>
    where TEntity : class, IScenarioEntity
    where TCreateRequest : class
    where TUpdateRequest : class
{
    protected readonly ILogger _logger;

    protected ScenarioServiceBase(ILogger logger)
    {
        _logger = logger;
    }

    public abstract Task<TEntity> CreateAsync(string scenarioId, TCreateRequest request);
    public abstract Task<TEntity?> GetAsync(string scenarioId, string id);
    public abstract Task<TEntity> UpdateAsync(string scenarioId, string id, TUpdateRequest request);
    public abstract Task DeleteAsync(string scenarioId, string id);
}