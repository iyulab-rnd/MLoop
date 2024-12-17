using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Services;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{
    private readonly JobService _jobService;

    public JobsController(JobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet("{jobId}")]
    public IActionResult GetStatus(string jobId)
    {
        var status = _jobService.GetJobStatus(jobId);
        return status == null ? NotFound() : Ok(status);
    }
}