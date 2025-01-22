namespace MLoop.Models;

/// <summary>
/// ML 작업의 타입을 정의합니다.
/// 워크플로우는 이 타입들 중 하나로 정의되며,
/// 실제 Job 실행 시 해당 타입으로 실행됩니다.
/// </summary>
public enum JobTypes
{
    /// <summary>
    /// 모델 학습 작업
    /// </summary>
    Train,

    /// <summary>
    /// 예측 작업
    /// </summary>
    Predict,

    /// <summary>
    /// 일반 목적 작업 (데이터 전처리, 평가, 분석 등)
    /// </summary>
    General
}