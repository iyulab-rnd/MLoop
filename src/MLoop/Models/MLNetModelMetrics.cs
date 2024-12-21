using System.Text.Json;

namespace MLoop.Models;

public class MLModelMetrics : Dictionary<string, double>
{
    public static async Task<MLModelMetrics> ParseForMLNetAsync(string modelPath)
    {
        var mbconfigPath = Path.Combine(modelPath, "Model", "Model.mbconfig");
        if (!File.Exists(mbconfigPath))
        {
            throw new FileNotFoundException("Model.mbconfig not found", mbconfigPath);
        }

        var metrics = new MLModelMetrics();

        var configJson = await File.ReadAllTextAsync(mbconfigPath);
        var config = JsonSerializer.Deserialize<JsonElement>(configJson);

        // RunHistory에서 trials 정보 추출
        if (config.TryGetProperty("RunHistory", out var runHistory) &&
            runHistory.TryGetProperty("Trials", out var trials))
        {
            var trialsList = trials.EnumerateArray()
                .Select(trial => new
                {
                    TrainerName = trial.GetProperty("TrainerName").GetString() ?? "",
                    Score = trial.GetProperty("Score").GetDouble(),
                    Runtime = trial.GetProperty("RuntimeInSeconds").GetDouble()
                })
                .OrderByDescending(t => t.Score)
                .ToList();

            // 메트릭 기록
            metrics.Clear(); // 기존 메트릭 클리어

            metrics["ModelsExplored"] = trialsList.Count;
            metrics["TotalRuntime"] = trialsList.Sum(t => t.Runtime);

            // 각 trial의 메트릭 기록
            for (int i = 0; i < trialsList.Count; i++)
            {
                var trial = trialsList[i];
                var rank = i + 1;

                // 스코어와 런타임 기록
                metrics[$"Rank{rank}_Score"] = trial.Score;
                metrics[$"Rank{rank}_Runtime"] = trial.Runtime;

                // 트레이너 이름은 문자열이므로 별도 딕셔너리에 저장
                metrics[$"Rank{rank}_Trainer_Id"] = 0; // 숫자형 필드 유지

                // 가장 좋은 모델의 메트릭을 BestScore로도 기록
                if (i == 0)
                {
                    metrics["BestScore"] = trial.Score;
                    metrics["BestRuntime"] = trial.Runtime;
                    metrics["BestTrainer_Id"] = 0; // 숫자형 필드 유지
                }
            }

            // 트레이너 정보를 포함한 전체 메트릭을 별도 JSON 파일로 저장
            var fullMetrics = new Dictionary<string, object>
            {
                ["ModelsExplored"] = trialsList.Count,
                ["TotalRuntime"] = trialsList.Sum(t => t.Runtime),
                ["BestScore"] = trialsList[0].Score,
                ["BestRuntime"] = trialsList[0].Runtime,
                ["BestTrainer"] = trialsList[0].TrainerName
            };

            // 각 순위별 상세 정보 추가
            for (int i = 0; i < trialsList.Count; i++)
            {
                var trial = trialsList[i];
                var rank = i + 1;
                fullMetrics[$"Rank{rank}_Score"] = trial.Score;
                fullMetrics[$"Rank{rank}_Runtime"] = trial.Runtime;
                fullMetrics[$"Rank{rank}_Trainer"] = trial.TrainerName;
            }

            // 훈련 설정 정보 추출
            if (config.TryGetProperty("TrainingOption", out var trainingOption) &&
                trainingOption.TryGetProperty("TrainingTime", out var trainingTime))
            {
                fullMetrics["ConfiguredTrainingTime"] = trainingTime.GetDouble();
                metrics["ConfiguredTrainingTime"] = trainingTime.GetDouble();
            }
        }

        return metrics;
    }
}