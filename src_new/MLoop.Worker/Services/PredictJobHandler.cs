using MLoop.Services;
using MLoop.Storages;

namespace MLoop.Worker.Services;

public class PredictJobHandler
{
    private readonly IFileStorage _storage;
    private readonly IMlnetRunner _runner;
    private readonly ScenarioManager _scenarioManager;

    public PredictJobHandler(IFileStorage storage, IMlnetRunner runner, ScenarioManager scenarioManager)
    {
        _storage = storage;
        _runner = runner;
        _scenarioManager = scenarioManager;
    }

    public void Handle(string scenarioId, string jobId)
    {
        var scenario = _scenarioManager.LoadScenarioAsync(scenarioId).Result;
        if (scenario.Models.Count == 0)
            throw new Exception("No models available for prediction");

        string modelName = scenario.Models.Last();
        string modelPath = Path.Combine(_storage.GetScenarioModelsDir(scenarioId), modelName);

        string dataDir = _storage.GetScenarioDataDir(scenarioId);
        var csvFiles = Directory.GetFiles(dataDir, "*.csv");
        if (csvFiles.Length == 0)
            throw new Exception("No CSV files found for prediction");

        var inputData = csvFiles.OrderByDescending(f => f).First();
        string resultPath = _storage.GetJobResultPath(jobId);

        _runner.Predict(modelPath, inputData, resultPath);
    }
}
