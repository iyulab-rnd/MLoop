using Microsoft.AspNetCore.Mvc;
using MLoop.Storages;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/data")]
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
    public async Task<IActionResult> UploadFiles(string scenarioId, List<IFormFile> files)
    {
        if (files == null || !files.Any())
            return BadRequest("업로드할 파일이 없습니다.");

        var uploadResults = new List<object>();
        var dataDir = _storage.GetScenarioDataDir(scenarioId);
        Directory.CreateDirectory(dataDir);

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                uploadResults.Add(new { fileName = file.FileName, error = "파일이 비어 있습니다." });
                continue;
            }

            try
            {
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

                uploadResults.Add(new
                {
                    fileName = Path.GetFileName(uniqueFilePath),
                    size = file.Length,
                    path = Path.GetRelativePath(dataDir, uniqueFilePath)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} for scenario {ScenarioId}", file.FileName, scenarioId);
                uploadResults.Add(new { fileName = file.FileName, error = "파일 업로드 중 오류가 발생했습니다." });
            }
        }

        return Ok(uploadResults);
    }

    [HttpDelete("{*filePath}")]
    public IActionResult DeleteFile(string scenarioId, string filePath)
    {
        try
        {
            if (filePath.Contains("..") || Path.IsPathRooted(filePath))
            {
                return BadRequest("잘못된 파일 경로입니다.");
            }

            var dataDir = _storage.GetScenarioDataDir(scenarioId);
            var fullPath = Path.Combine(dataDir, filePath);

            // 디렉토리 트래버설 방지
            if (!Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(dataDir)))
            {
                return BadRequest("잘못된 파일 경로입니다.");
            }

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("파일을 찾을 수 없습니다.");
            }

            System.IO.File.Delete(fullPath);
            return Ok(new { message = "파일이 성공적으로 삭제되었습니다." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath} for scenario {ScenarioId}", filePath, scenarioId);
            return StatusCode(500, "파일 삭제 중 오류가 발생했습니다.");
        }
    }
}
