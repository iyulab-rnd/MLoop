using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using MLoop.Storages;
using System.IO.Compression;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/scenarios/{scenarioId}/data")]
public class FileController : ControllerBase
{
    private readonly IFileStorage _storage;
    private readonly ILogger<FileController> _logger;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public FileController(IFileStorage storage, ILogger<FileController> logger)
    {
        _storage = storage;
        _logger = logger;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    private async Task EnsureDirectoryExistsAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            await Task.Run(() => Directory.CreateDirectory(directoryPath));
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListFiles(string scenarioId, [FromQuery] string? path)
    {
        try
        {
            var entries = await _storage.GetScenarioDataEntriesAsync(scenarioId, path);
            return Ok(entries);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing data files for scenario {ScenarioId} at path {Path}", scenarioId, path);
            return StatusCode(500, "Error listing files");
        }
    }

    [HttpGet("{*filePath}")]
    public async Task<IActionResult> GetFile(string scenarioId, string filePath)
    {
        try
        {
            var validationResult = _storage.ValidateAndGetFullPath(scenarioId, filePath);
            if (!validationResult.isValid)
            {
                return BadRequest(validationResult.error);
            }

            var fullPath = validationResult.fullPath!;
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File not found.");
            }

            var fileName = Path.GetFileName(fullPath);
            if (!_contentTypeProvider.TryGetContentType(fileName, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return new FileStreamResult(fileStream, contentType)
            {
                FileDownloadName = fileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {FilePath} for scenario {ScenarioId}", filePath, scenarioId);
            return StatusCode(500, "Error occurred while getting the file.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadFiles(string scenarioId, [FromQuery] string? path, List<IFormFile> files)
    {
        if (files == null || !files.Any())
            return BadRequest("No files to upload.");

        // Validate target path
        string targetDir = _storage.GetScenarioDataDir(scenarioId);
        if (!string.IsNullOrEmpty(path))
        {
            var validationResult = _storage.ValidateAndGetFullPath(scenarioId, path);
            if (!validationResult.isValid)
            {
                return BadRequest(validationResult.error);
            }
            targetDir = validationResult.fullPath!;
        }

        var uploadResults = new List<object>();
        await EnsureDirectoryExistsAsync(targetDir);

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                uploadResults.Add(new { fileName = file.FileName, error = "File is empty." });
                continue;
            }

            try
            {
                var uniqueFilePath = await GetUniqueFilePathAsync(targetDir, file.FileName);
                await EnsureDirectoryExistsAsync(Path.GetDirectoryName(uniqueFilePath)!);

                using (var stream = new FileStream(uniqueFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                uploadResults.Add(new
                {
                    fileName = Path.GetFileName(uniqueFilePath),
                    size = file.Length,
                    path = Path.GetRelativePath(_storage.GetScenarioDataDir(scenarioId), uniqueFilePath).Replace("\\", "/")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} for scenario {ScenarioId}", file.FileName, scenarioId);
                uploadResults.Add(new { fileName = file.FileName, error = "Error occurred while uploading file." });
            }
        }

        return Ok(uploadResults);
    }

    private async Task<string> GetUniqueFilePathAsync(string directory, string fileName)
    {
        var filePath = Path.Combine(directory, fileName);
        string uniqueFilePath = filePath;
        int counter = 1;

        while (await Task.Run(() => System.IO.File.Exists(uniqueFilePath)))
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            uniqueFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
            counter++;
        }

        return uniqueFilePath;
    }

    [HttpDelete("{*filePath}")]
    public async Task<IActionResult> DeleteFile(string scenarioId, string filePath)
    {
        try
        {
            var validationResult = _storage.ValidateAndGetFullPath(scenarioId, filePath);
            if (!validationResult.isValid)
            {
                return BadRequest(validationResult.error);
            }

            var fullPath = validationResult.fullPath!;
            if (!System.IO.File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                return NotFound("File or directory not found.");
            }

            await DeleteFileOrDirectoryAsync(fullPath);
            return Ok(new { message = "File successfully deleted." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath} for scenario {ScenarioId}", filePath, scenarioId);
            return StatusCode(500, "Error occurred while deleting the file.");
        }
    }

    private async Task DeleteFileOrDirectoryAsync(string path)
    {
        var attr = await Task.Run(() => System.IO.File.GetAttributes(path));
        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
        {
            // 폴더인 경우 하위 항목을 포함하여 삭제
            await Task.Run(() => Directory.Delete(path, recursive: true));
        }
        else
        {
            // 파일인 경우 단일 파일 삭제
            await Task.Run(() => System.IO.File.Delete(path));
        }
    }

    [HttpPost("unzip")]
    public async Task<IActionResult> UnzipFile(string scenarioId, [FromQuery] string path)
    {
        try
        {
            var validationResult = _storage.ValidateAndGetFullPath(scenarioId, path);
            if (!validationResult.isValid)
            {
                return BadRequest(validationResult.error);
            }

            var fullPath = validationResult.fullPath!;
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File not found.");
            }

            if (!Path.GetExtension(fullPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only ZIP files can be extracted.");
            }

            var extractResult = await ExtractZipFileAsync(scenarioId, fullPath);
            return Ok(new
            {
                message = "ZIP file successfully extracted.",
                extractedFiles = extractResult
            });
        }
        catch (InvalidDataException)
        {
            return BadRequest("Invalid ZIP file format.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unzipping file {FilePath} for scenario {ScenarioId}", path, scenarioId);
            return StatusCode(500, "Error occurred while extracting the ZIP file.");
        }
    }

    private async Task<List<string>> ExtractZipFileAsync(string scenarioId, string zipPath)
    {
        var extractPath = Path.GetDirectoryName(zipPath)!;
        var extractedFiles = new List<string>();

        using var archive = await Task.Run(() => System.IO.Compression.ZipFile.OpenRead(zipPath));
        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
            if (!destinationPath.StartsWith(Path.GetFullPath(extractPath)))
            {
                throw new InvalidOperationException("ZIP file contains invalid paths.");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                await EnsureDirectoryExistsAsync(destinationPath);
                continue;
            }

            await EnsureDirectoryExistsAsync(Path.GetDirectoryName(destinationPath)!);
            await Task.Run(() => entry.ExtractToFile(destinationPath, overwrite: true));
            extractedFiles.Add(Path.GetRelativePath(_storage.GetScenarioDataDir(scenarioId), destinationPath).Replace("\\", "/"));
        }

        return extractedFiles;
    }
}