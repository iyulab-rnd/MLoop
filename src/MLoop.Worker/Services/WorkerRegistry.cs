using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MLoop.Worker.Services;

public class WorkerRegistry
{
    private readonly ILogger<WorkerRegistry> _logger;
    private readonly ConcurrentDictionary<string, WorkerInfo> _workers;

    public WorkerRegistry(ILogger<WorkerRegistry> logger)
    {
        _logger = logger;
        _workers = new ConcurrentDictionary<string, WorkerInfo>();
    }

    public void RegisterWorker(string workerId, string machineName, string processId)
    {
        var info = new WorkerInfo
        {
            WorkerId = workerId,
            MachineName = machineName,
            ProcessId = processId,
            StartedAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow
        };

        if (_workers.TryAdd(workerId, info))
        {
            _logger.LogInformation(
                "Worker registered - ID: {WorkerId}, Machine: {MachineName}, PID: {ProcessId}",
                workerId, machineName, processId);
        }
    }

    public void UpdateHeartbeat(string workerId)
    {
        if (_workers.TryGetValue(workerId, out var info))
        {
            info.LastHeartbeat = DateTime.UtcNow;
            info.ActiveJobCount = GetActiveJobCount(workerId);
        }
    }

    public bool IsWorkerActive(string workerId)
    {
        if (_workers.TryGetValue(workerId, out var info))
        {
            var heartbeatAge = DateTime.UtcNow - info.LastHeartbeat;
            return heartbeatAge <= TimeSpan.FromMinutes(5);
        }
        return false;
    }

    private int GetActiveJobCount(string workerId)
    {
        // 실제로는 JobManager를 통해 현재 워커가 처리 중인 작업 수를 조회
        return 0;
    }
}

public class WorkerInfo
{
    public string WorkerId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string ProcessId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public int ActiveJobCount { get; set; }
}