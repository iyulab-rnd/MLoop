using MLoop.Services;
using MLoop.Storages;

namespace MLoop.Worker.Services;

public class TrainJobHandler
{
    private readonly IFileStorage _storage;
    private readonly IMlnetRunner _runner;
    private readonly ScenarioManager _scenarioManager;

    public TrainJobHandler(IFileStorage storage, IMlnetRunner runner, ScenarioManager scenarioManager)
    {
        _storage = storage;
        _runner = runner;
        _scenarioManager = scenarioManager;
    }

    public void Handle(string scenarioId)
    {
        string dataDir = _storage.GetScenarioDataDir(scenarioId);
        if (!Directory.Exists(dataDir))
            throw new Exception("No data found");

        var csvFiles = Directory.GetFiles(dataDir, "*.csv");
        if (csvFiles.Length == 0)
            throw new Exception("No CSV files found for training");

        var dataFile = csvFiles.OrderByDescending(f => f).First();
        string modelDir = _storage.GetScenarioModelsDir(scenarioId);
        Directory.CreateDirectory(modelDir);
        string modelFileName = $"model_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
        string modelPath = Path.Combine(modelDir, modelFileName);

        var scenario = _scenarioManager.LoadScenarioAsync(scenarioId).Result;

        // scenario.Command를 사용하여 Runner 호출
        _runner.TrainModel(dataFile, modelPath, scenario.Command);

        scenario.Models.Add(modelFileName);
        _scenarioManager.SaveScenarioAsync(scenarioId, scenario).Wait();
    }
}
