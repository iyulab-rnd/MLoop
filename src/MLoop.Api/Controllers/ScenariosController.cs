using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.OData.Query;
using MLoop.Api.Models;
using MLoop.Api.Services;
using MLoop.Models;
using MLoop.Storages;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class ScenariosController : ControllerBase
{
    private readonly ScenarioService _scenarioService;
    private readonly IFileStorage _storage;
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(
        ScenarioService scenarioService,
        IFileStorage storage,
        ILogger<ScenariosController> logger)
    {
        _scenarioService = scenarioService;
        _storage = storage;
        _logger = logger;
    }

    [HttpGet]
    [EnableQuery]
    public async Task<IActionResult> GetScenarios(ODataQueryOptions<ScenarioMetadata> queryOptions)
    {
        try
        {
            var scenarios = await _scenarioService.GetScenariosAsync();
            var validationSettings = new ODataValidationSettings
            {
                MaxTop = 100,
                AllowedQueryOptions = AllowedQueryOptions.Filter |
                                    AllowedQueryOptions.Select |
                                    AllowedQueryOptions.OrderBy |
                                    AllowedQueryOptions.Top |
                                    AllowedQueryOptions.Skip |
                                    AllowedQueryOptions.Count
            };

            queryOptions.Validate(validationSettings);

            var queryableScenarios = scenarios.AsQueryable();
            var results = queryOptions.ApplyTo(queryableScenarios);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            return BadRequest($"Error processing query: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateScenario([FromBody] CreateScenarioRequest request)
    {
        try
        {
            var scenario = await _scenarioService.CreateScenarioAsync(request);
            return CreatedAtAction(nameof(GetScenario), new { scenarioId = scenario.ScenarioId }, scenario);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid scenario creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scenario");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{scenarioId}")]
    public async Task<IActionResult> GetScenario(string scenarioId)
    {
        try
        {
            var scenario = await _scenarioService.GetScenarioAsync(scenarioId);
            if (scenario == null)
                return NotFound();
            return Ok(scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{scenarioId}")]
    public async Task<IActionResult> UpdateScenario(string scenarioId, [FromBody] CreateScenarioRequest request)
    {
        try
        {
            await _scenarioService.UpdateScenarioAsync(scenarioId, request);
            var updatedScenario = await _scenarioService.GetScenarioAsync(scenarioId);
            return Ok(updatedScenario);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid scenario update request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{scenarioId}")]
    public async Task<IActionResult> DeleteScenario(string scenarioId)
    {
        try
        {
            var scenario = await _scenarioService.GetScenarioAsync(scenarioId);
            if (scenario == null)
                return NotFound();

            await _scenarioService.DeleteScenarioAsync(scenarioId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{scenarioId}/train")]
    public async Task<IActionResult> Train(string scenarioId)
    {
        try
        {
            // 필수 조건 검증
            var validationResult = await ValidateTrainingPrerequisites(scenarioId);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ErrorMessage);
            }

            var scenario = await _scenarioService.GetScenarioAsync(scenarioId);
            if (scenario == null)
                return NotFound("Scenario not found");

            var r = await _scenarioService.CreateTrainJobAsync(scenarioId);
            return Ok(new { r.jobId, r.status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating train job for scenario {ScenarioId}", scenarioId);
            return BadRequest(ex.Message);
        }
    }

    private async Task<RequestValidationResult> ValidateTrainingPrerequisites(string scenarioId)
    {
        var scenarioBaseDir = _storage.GetScenarioBaseDir(scenarioId);

        // 데이터 파일 존재 여부 확인
        var dataDir = Path.Combine(scenarioBaseDir, "data");
        if (!Directory.Exists(dataDir) || !Directory.EnumerateFiles(dataDir).Any())
        {
            return RequestValidationResult.Fail("No data files found. Please upload data files before starting training.");
        }

        // train.yaml 파일 존재 여부 확인
        var workflowsDir = Path.Combine(scenarioBaseDir, "workflows");
        var trainWorkflowPath = Path.Combine(workflowsDir, "train.yaml");
        if (!System.IO.File.Exists(trainWorkflowPath))
        {
            return RequestValidationResult.Fail("Training workflow (train.yaml) not found. Please create training workflow first.");
        }

        // train.yaml 파일 내용 검증
        try
        {
            var yamlContent = await System.IO.File.ReadAllTextAsync(trainWorkflowPath);
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                return RequestValidationResult.Fail("Training workflow file is empty.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading training workflow file for scenario {ScenarioId}", scenarioId);
            return RequestValidationResult.Fail("Error reading training workflow file.");
        }

        return RequestValidationResult.Success();
    }
}