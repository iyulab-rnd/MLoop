namespace MLoop.Worker.Steps.Registry;

public class StepRegistry
{
    private readonly Dictionary<string, IStepRunner> _runners;

    public StepRegistry(IEnumerable<IStepRunner> runners)
    {
        _runners = runners.ToDictionary(r => r.Type);
    }

    public IStepRunner GetRunner(string type)
    {
        if (!_runners.TryGetValue(type, out var runner))
        {
            throw new KeyNotFoundException($"No runner found for step type: {type}");
        }
        return runner;
    }

    public bool HasRunner(string type) => _runners.ContainsKey(type);

    public IEnumerable<string> GetSupportedTypes() => _runners.Keys;
}