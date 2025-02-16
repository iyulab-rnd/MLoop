using MLoop.Api.Models.Datasets;
using MLoop.Models.Datasets;
using MLoop.Services;

namespace MLoop.Api.Services;

public class DatasetService
{
    private readonly DatasetManager _datasetManager;
    private readonly DatasetHandler _datasetHandler;
    private readonly ILogger<DatasetService> _logger;

    public DatasetService(
        DatasetManager datasetManager,
        DatasetHandler datasetHandler,
        ILogger<DatasetService> logger)
    {
        _datasetManager = datasetManager;
        _datasetHandler = datasetHandler;
        _logger = logger;
    }

    public async Task<Dataset> CreateAsync(CreateDatasetRequest request)
    {
        if (await _datasetManager.ExistsAsync(request.Name))
            throw new ValidationException($"Dataset with name '{request.Name}' already exists");

        var dataset = await _datasetHandler.InitializeDatasetAsync(request);
        _logger.LogInformation("Created new dataset: {DatasetName}", dataset.Name);
        return dataset;
    }

    public async Task<Dataset?> GetAsync(string name)
    {
        return await _datasetManager.LoadAsync(name);
    }

    public async Task<IEnumerable<Dataset>> GetAllDatasetsAsync()
    {
        return await _datasetManager.GetAllDatasetsAsync();
    }

    public async Task<Dataset> UpdateAsync(string name, UpdateDatasetRequest request)
    {
        var dataset = await _datasetHandler.UpdateDatasetAsync(name, request);
        _logger.LogInformation("Updated dataset {DatasetName}", name);
        return dataset;
    }

    public async Task DeleteAsync(string name)
    {
        await _datasetManager.DeleteAsync(name);
        _logger.LogInformation("Deleted dataset {DatasetName}", name);
    }

    public async Task UpdateSizeAsync(string name)
    {
        await _datasetManager.UpdateSizeAsync(name);
    }
}