using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MLoop.Actions;
using MLoop.Internal;
using MLoop.Models;
using MLoop.Utils;
using System.Text.Json.Nodes;

namespace MLoop.Services;

public class MLoopApiService
{
    private readonly ILogger<MLoopApiService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MLoopOptions _options;

    public MLoopApiService(ILogger<MLoopApiService> logger,
        IServiceProvider serviceProvider,
        IOptions<MLoopOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    private string GetScenarioPath(string scenarioName)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var scenarioPath = Path.Combine(basePath, scenarioName);
        return scenarioPath;
    }

    private string GetScenarioDataPath(MLScenario scenario)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var dataPath = scenario.DataPath ?? Path.Combine(basePath, scenario.Name, "data.csv");
        return Path.Combine(basePath, scenario.Name, dataPath);
    }

    public async IAsyncEnumerable<MLScenario> GetMLScenariosAsync()
    {
        var basePath = Path.GetFullPath(_options.Path);
        var directories = Directory.GetDirectories(basePath);

        foreach (var directory in directories)
        {
            MLScenario? scenario = null;
            try
            {
                scenario = await MLoopFactory.GetMLScenarioAsync(directory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scenario from directory {Directory}", directory);
            }
            if (scenario == null) continue;

            yield return scenario;
        }
    }

    public async Task<MLScenario?> GetMLScenarioAsync(string scenarioName)
    {
        var scenarioPath = GetScenarioPath(scenarioName);
        if (!Directory.Exists(scenarioPath))
        {
            return null;
        }

        return await MLoopFactory.GetMLScenarioAsync(scenarioPath);
    }

    public async Task<MLTrainActionResult> BuildNewModelAsync(MLScenario scenario, TrainOptions? trainOptions = null)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var modelKey = RandomHelper.RandomString(8);
        var modelPath = Path.Combine(basePath, scenario.Name, modelKey);

        if (scenario.DataPath == null || !File.Exists(scenario.DataPath))
            throw new InvalidDataException($"cannot find data, scenario: {scenario.Name}");

        var action = await MLoopFactory.BuildDefaultActionAsync(modelPath, trainOptions);

        var executor = _serviceProvider.GetRequiredService<MLTrainActionExecutor>();
        return await executor.ExecuteAsync(action);
    }

    public async IAsyncEnumerable<MLModel> GetModelsAsync(MLScenario scenario)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var scenarioPath = Path.Combine(basePath, scenario.Name);
        var directories = Directory.GetDirectories(scenarioPath);

        foreach (var directory in directories)
        {
            MLModel? model = null;

            try
            {
                var modelName = Path.GetFileName(directory);
                model = await MLoopFactory.CreateMLModelAsync(modelName, directory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating model from directory {Directory}", directory);
            }
            if (model == null) continue;
            yield return model;
        }
    }

    public Task<MLModel> GetMLModelAsync(MLScenario scenario, string modelName)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var modelPath = Path.Combine(basePath, scenario.Name, modelName);
        if (!Directory.Exists(modelPath))
        {
            throw new EntryPointNotFoundException($"Model not found. {modelName}");
        }

        return MLoopFactory.CreateMLModelAsync(modelName, modelPath);
    }

    public async Task<string> GetMLModelLogAsync(MLScenario scenario, string modelName)
    {
        var basePath = Path.GetFullPath(_options.Path);
        var modelPath = Path.Combine(basePath, scenario.Name, modelName);
        if (!Directory.Exists(modelPath))
        {
            throw new EntryPointNotFoundException($"Model not found. {modelName}");
        }

        var logPath = Path.Combine(modelPath, "console.log");
        if (!File.Exists(logPath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(logPath);
    }

    internal async Task<MLModel?> GetPredictionModelAsync(MLScenario scenario)
    {
        MLModel? lastModel = null;

        await foreach (var model in GetModelsAsync(scenario))
        {
            if (model.TrainState == null || model.TrainState.TrainingCompleted == null) continue;

            if (lastModel == null || model.TrainState.TrainingCompleted > lastModel.TrainState!.TrainingCompleted)
            {
                lastModel = model;
            }
        }

        return lastModel;
    }

    public async Task<int> SubmitDataAsync(MLScenario scenario, string text)
    {   
        var hasHeader = CsvUtils.HasHeader(text);
        var config = CsvUtils.GetDefaultConfiguration(hasHeader);
        var newRecords = CsvUtils.ParseAndValidateData(text, config);
        if (newRecords.Count == 0)
        {
            throw new InvalidDataException("No valid data found in the input.");
        }

        var dataPath = GetScenarioDataPath(scenario);
        var rowCount = await ProcessRecordsAsync(dataPath, newRecords, config);
        return rowCount;
    }

    public async Task<int> SubmitDataAsync(MLScenario scenario, JsonNode json)
    {   
        var config = CsvUtils.GetDefaultConfiguration();
        var jsonRecords = CsvUtils.ConvertJsonToCsvRecords(json);
        if (jsonRecords.Count == 0)
        {
            throw new InvalidDataException("No valid data found in the input JSON.");
        }

        var dataPath = GetScenarioDataPath(scenario);
        var rowCount = await ProcessRecordsAsync(dataPath, jsonRecords, config);
        return rowCount;
    }

    private async Task<int> ProcessRecordsAsync(string dataPath, List<dynamic> records, CsvConfiguration config)
    {
        if (!File.Exists(dataPath))
        {
            _logger.LogInformation("Creating new data file {DataPath}", dataPath);
            return await CsvUtils.WriteDataToFileAsync(dataPath, records, config);
        }
        else
        {
            _logger.LogInformation("Appending data to existing file {DataPath}", dataPath);
            return await CsvUtils.AppendDataToFileAsync(dataPath, records, config);
        }
    }

    public async Task<string> SubmitPredictDataAsync(MLScenario scenario, string text)
    {
        var hasHeader = CsvUtils.HasHeader(text);
        var config = CsvUtils.GetDefaultConfiguration(hasHeader);
        var records = CsvUtils.ParseAndValidateData(text, config);
        if (records.Count == 0)
        {
            throw new InvalidDataException("No valid data found in the input.");
        }

        return await SubmitPredictDataAsync(scenario, records, config);
    }

    public async Task<string> SubmitPredictDataAsync(MLScenario scenario, JsonNode json)
    {
        var jsonRecords = CsvUtils.ConvertJsonToCsvRecords(json);
        if (jsonRecords.Count == 0)
        {
            throw new InvalidDataException("No valid data found in the input JSON.");
        }

        var config = CsvUtils.GetDefaultConfiguration();
        return await SubmitPredictDataAsync(scenario, jsonRecords, config);
    }

    private async Task<string> SubmitPredictDataAsync(MLScenario scenario, List<dynamic> records, CsvConfiguration config)
    {
        var scenarioPath = GetScenarioPath(scenario.Name);
        var inputsPath = Path.Combine(scenarioPath, "inputs");
        var inputPath = GetNextInputPath(inputsPath);

        _logger.LogInformation("Creating new input file {InputPath}", inputPath);
        await CsvUtils.WriteDataToFileAsync(inputPath, records, config);

        var inputFileName = Path.GetFileNameWithoutExtension(inputPath);
        return inputFileName;
    }

    private static string GetNextInputPath(string basePath)
    {
        string[] existingFiles = Directory.GetFiles(basePath, "input_*.csv");

        int maxNumber = 0;
        foreach (string file in existingFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (int.TryParse(fileName.Split('_')[1], out int number))
            {
                maxNumber = Math.Max(maxNumber, number);
            }
        }

        int nextNumber = maxNumber + 1;
        return Path.Combine(basePath, $"input_{nextNumber}.csv");
    }

    public async Task<string> GetPredictResultAsync(MLScenario scenario, string inputName)
    {
        var scenarioPath = GetScenarioPath(scenario.Name);
        var inputsPath = Path.Combine(scenarioPath, "inputs");
        var predictedPath = Path.Combine(inputsPath, $"{inputName}-predicted.csv");

        if (!File.Exists(predictedPath))
        {
            var inputPath = Path.Combine(inputsPath, $"{inputName}.csv");
            if (File.Exists(inputPath))
            {
                throw new MLoopException("Forecast in progress. Please wait a moment");
            }
            else
            {
                throw new MLoopException($"Invalid prediction result request. Cannot find {inputName}.");
            }
        }

        return await RetryFile.ReadAllTextAsync(predictedPath) ?? string.Empty;
    }
}
