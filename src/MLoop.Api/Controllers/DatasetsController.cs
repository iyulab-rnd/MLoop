using Microsoft.AspNetCore.Mvc;
using MLoop.Api.Models.Datasets;
using MLoop.Storages;

namespace MLoop.Api.Controllers;

[ApiController]
[Route("/api/datasets")]
public class DatasetsController : ControllerBase
{
    private readonly DatasetService _datasetService;
    private readonly IFileStorage _storage;
    private readonly ILogger<DatasetsController> _logger;

    public DatasetsController(
        DatasetService datasetService,
        IFileStorage storage,
        ILogger<DatasetsController> logger)
    {
        _datasetService = datasetService;
        _storage = storage;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDatasets()
    {
        try
        {
            var datasets = await _datasetService.GetAllDatasetsAsync();
            return Ok(datasets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving datasets");
            return StatusCode(500, "Error retrieving datasets");
        }
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetDataset(string name)
    {
        try
        {
            var dataset = await _datasetService.GetAsync(name);
            if (dataset == null)
                return NotFound($"Dataset {name} not found");
            return Ok(dataset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dataset {DatasetName}", name);
            return StatusCode(500, "Error retrieving dataset");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDataset([FromBody] CreateDatasetRequest request)
    {
        try
        {
            var dataset = await _datasetService.CreateAsync(request);
            return CreatedAtAction(
                nameof(GetDataset),
                new { name = dataset.Name },
                dataset);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dataset");
            return StatusCode(500, "Error creating dataset");
        }
    }

    [HttpPut("{name}")]
    public async Task<IActionResult> UpdateDataset(string name, [FromBody] UpdateDatasetRequest request)
    {
        try
        {
            var dataset = await _datasetService.UpdateAsync(name, request);
            return Ok(dataset);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Dataset {name} not found");
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dataset {DatasetName}", name);
            return StatusCode(500, "Error updating dataset");
        }
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteDataset(string name)
    {
        try
        {
            var dataset = await _datasetService.GetAsync(name);
            if (dataset == null)
                return NotFound($"Dataset {name} not found");

            await _datasetService.DeleteAsync(name);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dataset {DatasetName}", name);
            return StatusCode(500, "Error deleting dataset");
        }
    }

    [HttpGet("{name}/files")]
    public async Task<IActionResult> ListFiles(string name, [FromQuery] string? path)
    {
        try
        {
            var entries = await _storage.GetDatasetEntriesAsync(name, path);
            return Ok(entries);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files for dataset {DatasetName} at path {Path}", name, path);
            return StatusCode(500, "Error listing files");
        }
    }

    [HttpGet("{name}/files/{*filePath}")]
    public async Task<IActionResult> GetFile(string name, string filePath)
    {
        try
        {
            var validationResult = _storage.ValidateDatasetPath(name, filePath);
            if (!validationResult.isValid)
            {
                return BadRequest(validationResult.error);
            }

            var fullPath = validationResult.fullPath!;
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File not found");
            }

            var fileName = Path.GetFileName(fullPath);
            var contentType = GetContentType(fileName);

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return File(fileStream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {FilePath} for dataset {DatasetName}", filePath, name);
            return StatusCode(500, "Error getting file");
        }
    }

    [HttpPost("{name}/files")]
    [RequestSizeLimit(200 * 1024 * 1024)] // 200MB limit
    public async Task<IActionResult> UploadFiles(string name, [FromQuery] string? path, List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files to upload");

        try
        {
            string targetDir = _storage.GetDatasetDataDir(name);
            if (!string.IsNullOrEmpty(path))
            {
                var (isValid, fullPath, error) = _storage.ValidateDatasetPath(name, path);
                if (!isValid)
                {
                    return BadRequest(error);
                }
                targetDir = fullPath!;
            }

            var uploadResults = new List<object>();
            await EnsureDirectoryExistsAsync(targetDir);

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    uploadResults.Add(new { fileName = file.FileName, error = "File is empty" });
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
                        path = Path.GetRelativePath(_storage.GetDatasetDataDir(name), uniqueFilePath).Replace("\\", "/")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file {FileName} for dataset {DatasetName}", file.FileName, name);
                    uploadResults.Add(new { fileName = file.FileName, error = "Error uploading file" });
                }
            }

            // 파일 업로드 후 데이터셋 크기 업데이트
            await _datasetService.UpdateSizeAsync(name);

            return Ok(uploadResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading files to dataset {DatasetName}", name);
            return StatusCode(500, "Error uploading files");
        }
    }

    [HttpDelete("{name}/files/{*filePath}")]
    public async Task<IActionResult> DeleteFile(string name, string filePath)
    {
        try
        {
            var validationResult = _storage.ValidateDatasetPath(name, filePath);
            if (!validationResult.isValid)
            {
                return BadRequest(validationResult.error);
            }

            var fullPath = validationResult.fullPath!;
            if (!System.IO.File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                return NotFound("File or directory not found");
            }

            await DeleteFileOrDirectoryAsync(fullPath);

            // 파일 삭제 후 데이터셋 크기 업데이트
            await _datasetService.UpdateSizeAsync(name);

            return Ok(new { message = "File successfully deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath} for dataset {DatasetName}", filePath, name);
            return StatusCode(500, "Error deleting file");
        }
    }

    private static async Task EnsureDirectoryExistsAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            await Task.Run(() => Directory.CreateDirectory(directoryPath));
        }
    }

    private static async Task<string> GetUniqueFilePathAsync(string directory, string fileName)
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

    private static async Task DeleteFileOrDirectoryAsync(string path)
    {
        var attr = await Task.Run(() => System.IO.File.GetAttributes(path));
        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
        {
            await Task.Run(() => Directory.Delete(path, recursive: true));
        }
        else
        {
            await Task.Run(() => System.IO.File.Delete(path));
        }
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLower() switch
        {
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".tsv" => "text/tab-separated-values",
            ".json" => "application/json",
            ".yaml" or ".yml" => "text/yaml",
            _ => "application/octet-stream"
        };
    }
}