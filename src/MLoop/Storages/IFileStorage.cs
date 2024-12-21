namespace MLoop.Storages;

public interface IFileStorage
{
    Task<IEnumerable<string>> GetScenarioIdsAsync();
    string GetScenarioBaseDir(string scenarioId);
    string GetScenarioDataDir(string scenarioId);
    string GetScenarioModelsDir(string scenarioId);
    string GetScenarioMetadataPath(string scenarioId);
    Task<IEnumerable<FileInfo>> GetScenarioDataFilesAsync(string scenarioId);

    string GetModelPath(string scenarioId, string modelId);
    string GetScenarioJobsDir(string scenarioId);
    string GetJobPath(string scenarioId, string jobId);
    string GetJobResultPath(string scenarioId, string jobId);
    string GetJobLogsPath(string scenarioId, string jobId);
    Task<IEnumerable<FileInfo>> GetScenarioJobFilesAsync(string scenarioId);

    string GetPredictionDir(string scenarioId, string predictionId);
    string GetPredictionInputPath(string scenarioId, string predictionId, string extension);
    string GetPredictionResultPath(string scenarioId, string predictionId);

    string GetPredictionsDir(string scenarioId); // 시나리오 전체 예측 디렉토리
    Task<IEnumerable<FileInfo>> GetPredictionFiles(string scenarioId, string modelId); // 특정 모델의 예측 파일 목록

}