using Microsoft.Extensions.Logging;
using MLoop.Models.Datasets;

namespace MLoop.Services;

public class DatasetManager
{
    private readonly IFileStorage _storage;
    private readonly ILogger<DatasetManager> _logger;

    public DatasetManager(IFileStorage storage, ILogger<DatasetManager> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<Dataset?> LoadAsync(string name)
    {
        string path = _storage.GetDatasetMetadataPath(name);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path);
        var dataset = JsonHelper.Deserialize<Dataset>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize dataset metadata for {name}");

        dataset.Name = name;
        return dataset;
    }

    public async Task SaveAsync(Dataset dataset)
    {
        string path = _storage.GetDatasetMetadataPath(dataset.Name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonHelper.Serialize(dataset);
        await File.WriteAllTextAsync(path, json);

        _logger.LogInformation("Saved dataset {DatasetName}", dataset.Name);
    }

    public async Task DeleteAsync(string name)
    {
        var datasetDir = _storage.GetDatasetPath(name);
        if (Directory.Exists(datasetDir))
        {
            Directory.Delete(datasetDir, recursive: true);
            _logger.LogInformation("Deleted dataset {DatasetName} and all related files", name);
        }
    }

    public Task<bool> ExistsAsync(string name)
    {
        var path = _storage.GetDatasetMetadataPath(name);
        return Task.FromResult(File.Exists(path));
    }

    public async Task<List<Dataset>> GetAllDatasetsAsync()
    {
        var datasets = new List<Dataset>();
        var names = await _storage.GetDatasetNamesAsync();

        foreach (var name in names)
        {
            try
            {
                var dataset = await LoadAsync(name);
                if (dataset != null)
                {
                    datasets.Add(dataset);
                    _logger.LogInformation("Loaded dataset: {DatasetName}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dataset {DatasetName}", name);
            }
        }

        return datasets;
    }

    public async Task UpdateSizeAsync(string name)
    {
        var dataset = await LoadAsync(name)
            ?? throw new KeyNotFoundException($"Dataset {name} not found");

        var dataDir = _storage.GetDatasetDataDir(name);
        if (Directory.Exists(dataDir))
        {
            dataset.Size = await Task.Run(() =>
                new DirectoryInfo(dataDir)
                    .GetFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length));

            await SaveAsync(dataset);
        }
    }
}