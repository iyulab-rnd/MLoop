using MLoop.Models.Jobs;

namespace MLoop.Base;

/// <summary>
/// Job 처리에 대한 공통 인터페이스 
/// Worker가 구현하고 API가 참조할 수 있는 인터페이스
/// </summary>
public interface IJobProcessor
{
    Task<bool> ProcessAsync(MLJob job, CancellationToken cancellationToken);
    Task<bool> CancelAsync(MLJob job);
}
