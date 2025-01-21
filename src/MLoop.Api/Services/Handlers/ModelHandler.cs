using MLoop.Base;
using MLoop.Models;
using MLoop.Storages;

namespace MLoop.Api.Services.Handlers;

public class ModelHandler : HandlerBase<MLModel>
{
    private readonly IFileStorage _storage;

    public ModelHandler(
        IFileStorage storage,
        ILogger<ModelHandler> logger) : base(logger)
    {
        _storage = storage;
    }

    public override async Task<MLModel> ProcessAsync(MLModel model)
    {
        var modelPath = Path.Combine(_storage.GetModelPath(model.ScenarioId, model.ModelId), "model.json");
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);

        await File.WriteAllTextAsync(modelPath, JsonHelper.Serialize(model));
        return model;
    }

    public override Task ValidateAsync(MLModel model)
    {
        if (string.IsNullOrEmpty(model.ModelId))
            throw new ValidationException("ModelId is required");

        if (string.IsNullOrEmpty(model.MLType))
            throw new ValidationException("MLType is required");

        if (string.IsNullOrEmpty(model.Command))
            throw new ValidationException("Command is required");

        if (string.IsNullOrEmpty(model.ScenarioId))
            throw new ValidationException("ScenarioId is required");

        return Task.CompletedTask;
    }
}
