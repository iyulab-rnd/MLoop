namespace MLoop.Services;

public interface IMlnetRunner
{
    void TrainModel(string dataPath, string outputModelPath, string scenarioCommand);
    void Predict(string modelPath, string inputDataPath, string outputResultPath);
}
