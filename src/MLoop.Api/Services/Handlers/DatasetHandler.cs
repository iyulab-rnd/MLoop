using MLoop.Api.Models.Datasets;
using MLoop.Models.Datasets;
using MLoop.Services;

namespace MLoop.Api.Services.Handlers;

public class DatasetHandler
{
    private readonly DatasetManager _datasetManager;
    private readonly ILogger<DatasetHandler> _logger;

    public DatasetHandler(
        DatasetManager datasetManager,
        ILogger<DatasetHandler> logger)
    {
        _datasetManager = datasetManager;
        _logger = logger;
    }

    public async Task<Dataset> ProcessAsync(Dataset dataset)
    {
        await _datasetManager.SaveAsync(dataset);
        return dataset;
    }

    public void ValidateDataset(Dataset dataset)
    {
        if (!dataset.ValidateName())
            throw new ValidationException($"Invalid dataset name: {dataset.Name}. Name must start with a letter and contain only letters, numbers, hyphens, and underscores.");

        if (dataset.Tags != null && dataset.Tags.Count > 10)
            throw new ValidationException("Maximum 10 tags are allowed");
    }

    public async Task<Dataset> InitializeDatasetAsync(CreateDatasetRequest request)
    {
        var dataset = new Dataset
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Tags = request.Tags?.Select(t => t.Trim().ToLower()).ToList() ?? [],
            CreatedAt = DateTime.UtcNow
        };

        ValidateDataset(dataset);
        return await ProcessAsync(dataset);
    }

    public async Task<Dataset> UpdateDatasetAsync(string name, UpdateDatasetRequest request)
    {
        var dataset = await _datasetManager.LoadAsync(name)
            ?? throw new KeyNotFoundException($"Dataset {name} not found");

        if (request.Description != null)
            dataset.Description = request.Description.Trim();

        if (request.Tags != null)
            dataset.Tags = request.Tags.Select(t => t.Trim().ToLower()).ToList();

        ValidateDataset(dataset);
        return await ProcessAsync(dataset);
    }
}