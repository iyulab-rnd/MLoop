using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Services;
using MLoop.Models.Jobs;
using MLoop.Api.Models.Jobs;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/jobs")]
public class JobsController : ControllerBase
{
    private readonly JobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        JobService jobService,
        ILogger<JobsController> logger)
    {
        _jobService = jobService;
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
            var job = await _jobService.GetAsync(scenarioId, jobId);
            if (job == null)
                return NotFound();
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob(string scenarioId, [FromBody] CreateJobRequest request)
    {
        try
        {
            var job = await _jobService.CreateAsync(scenarioId, request);
            return CreatedAtAction(nameof(GetJobStatus), new { scenarioId, jobId = job.JobId }, job);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job in scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{jobId}")]
    public async Task<IActionResult> UpdateJob(string scenarioId, string jobId, [FromBody] UpdateJobRequest request)
    {
        try
        {
            var job = await _jobService.UpdateAsync(scenarioId, jobId, request);
            return Ok(job);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
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
                return NoContent();
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs for job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{jobId}/result")]
    public async Task<IActionResult> GetJobResult(string scenarioId, string jobId)
    {
        try
        {
            var result = await _jobService.GetJobResultAsync(scenarioId, jobId);
            if (result == null)
                return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving result for job {JobId} in scenario {ScenarioId}", jobId, scenarioId);
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
                await _jobService.DeleteAsync(scenarioId, job.JobId);
                _logger.LogInformation(
                    "Cleaned up job {JobId} for scenario {ScenarioId}",
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