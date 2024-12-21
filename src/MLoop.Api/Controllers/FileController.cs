using Microsoft.AspNetCore.Mvc;
using MLoop.Storages;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("scenarios/{scenarioId}/data")]
public class FileController : ControllerBase
{
    private readonly IFileStorage _storage;
    private readonly ILogger<FileController> _logger;

    public FileController(IFileStorage storage, ILogger<FileController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListFiles(string scenarioId)
    {
        try
        {
            var files = await _storage.GetScenarioDataFilesAsync(scenarioId);
            var dataDir = _storage.GetScenarioDataDir(scenarioId);

            var fileList = files.Select(f => new
            {
                name = Path.GetFileName(f.Name),
                path = Path.GetRelativePath(dataDir, f.FullName),
                size = f.Length,
                lastModified = f.LastWriteTimeUtc
            });

            return Ok(fileList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing data files for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Error listing files");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(string scenarioId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file was uploaded.");

        try
        {
            var dataDir = _storage.GetScenarioDataDir(scenarioId);
            Directory.CreateDirectory(dataDir);

            var filePath = Path.Combine(dataDir, file.FileName);

            // 파일명 중복 처리
            string uniqueFilePath = filePath;
            int counter = 1;
            while (System.IO.File.Exists(uniqueFilePath))
            {
                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                string extension = Path.GetExtension(file.FileName);
                uniqueFilePath = Path.Combine(dataDir, $"{fileName}_{counter}{extension}");
                counter++;
            }

            using (var stream = new FileStream(uniqueFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new
            {
                fileName = Path.GetFileName(uniqueFilePath),
                size = file.Length,
                path = Path.GetRelativePath(dataDir, uniqueFilePath)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, "Error uploading file");
        }
    }

    [HttpDelete("{*filePath}")]
    public IActionResult DeleteFile(string scenarioId, string filePath)
    {
        try
        {
            if (filePath.Contains("..") || Path.IsPathRooted(filePath))
            {
                return BadRequest("Invalid file path");
            }

            var dataDir = _storage.GetScenarioDataDir(scenarioId);
            var fullPath = Path.Combine(dataDir, filePath);

            // Prevent directory traversal
            if (!Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(dataDir)))
            {
                return BadRequest("Invalid file path");
            }

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File not found");
            }

            System.IO.File.Delete(fullPath);
            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath} for scenario {ScenarioId}", filePath, scenarioId);
            return StatusCode(500, "Error deleting file");
        }
    }
}