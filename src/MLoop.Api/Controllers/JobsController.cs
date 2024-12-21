using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Services;
using MLoop.Models.Jobs;
using MLoop.Storages;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("scenarios/{scenarioId}/jobs")]
public class JobsController : ControllerBase
{
    private readonly JobService _jobService;
    private readonly IFileStorage _storage;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        JobService jobService,
        IFileStorage storage,
        ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _storage = storage;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(string scenarioId)
    {
        try
        {
            var jobs = await _jobService.GetScenarioJobsAsync(scenarioId);
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJobStatus(string scenarioId, string jobId)
    {
        try
        {
            var status = await _jobService.GetJobStatusAsync(scenarioId, jobId);
            if (status == null)
                return NotFound();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{jobId}/cancel")]
    public async Task<IActionResult> CancelJob(string scenarioId, string jobId)
    {
        try
        {
            await _jobService.CancelJobAsync(scenarioId, jobId);
            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{jobId}/logs")]
    public async Task<IActionResult> GetJobLogs(string scenarioId, string jobId)
    {
        try
        {
            var logs = await _jobService.GetJobLogsAsync(scenarioId, jobId);
            if (logs == null)
                return NotFound();
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs for job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupJobs(string scenarioId)
    {
        try
        {
            var jobs = await _jobService.GetScenarioJobsAsync(scenarioId);
            var completedJobs = jobs.Where(j =>
                j.Status == MLJobStatus.Completed || j.Status == MLJobStatus.Failed);

            foreach (var job in completedJobs)
            {
                // 작업 파일 삭제
                var jobPath = _storage.GetJobPath(scenarioId, job.JobId);
                var resultPath = _storage.GetJobResultPath(scenarioId, job.JobId);
                var logsPath = _storage.GetJobLogsPath(scenarioId, job.JobId);

                if (System.IO.File.Exists(jobPath))
                    System.IO.File.Delete(jobPath);
                if (System.IO.File.Exists(resultPath))
                    System.IO.File.Delete(resultPath);
                if (System.IO.File.Exists(logsPath))
                    System.IO.File.Delete(logsPath);

                _logger.LogInformation(
                    "Cleaned up job {JobId} files for scenario {ScenarioId}",
                    job.JobId, scenarioId);
            }

            return Ok(new
            {
                message = "Completed jobs cleaned up successfully",
                cleanedCount = completedJobs.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up jobs for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new { message = "Error cleaning up jobs" });
        }
    }
}