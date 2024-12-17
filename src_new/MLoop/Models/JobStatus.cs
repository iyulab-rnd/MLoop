namespace MLoop.Models;

public class JobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;  // Train, Predict
    public string Status { get; set; } = string.Empty;   // Waiting, Running, Completed, Failed
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Command { get; set; }                 // 실행할 명령어 (train에서 사용)
    public string? ModelId { get; set; }                 // 예측에 사용할 모델 ID
    public string? ErrorMessage { get; set; }            // 실패 시 에러 메시지
}