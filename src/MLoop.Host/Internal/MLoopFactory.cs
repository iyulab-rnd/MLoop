using CsvHelper.Configuration;
using CsvHelper;
using MLoop.Models;
using System.Globalization;
using System.Text.Json;
using MLoop.SystemText.Json;
using Microsoft.VisualBasic;
using MLoop.Actions;
using System.Diagnostics;

namespace MLoop.Internal;

internal class MLoopFactory
{
    internal static async Task<MLScenario> GetMLScenarioAsync(string directory)
    {
        var dir = new DirectoryInfo(directory);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Directory {directory} not found.");
        }

        var metaPath = Path.Combine(directory, "meta.json");
        if (!File.Exists(metaPath))
        {
            throw new FileNotFoundException($"File {metaPath} not found, from {directory}");
        }

        var meta = await File.ReadAllTextAsync(metaPath);
        var scenario = JsonHelper.Deserialize<MLScenario>(meta)
            ?? throw new JsonException($"Failed to deserialize {metaPath}, from {directory}");

        if (string.IsNullOrEmpty(scenario.Name)) scenario.Name = dir.Name;

        if (string.IsNullOrEmpty(scenario.DataPath))
        {
            var trainPath = Path.Combine(directory, "data.csv");
            if (File.Exists(trainPath)) scenario.DataPath = trainPath;
        }

        return await Task.FromResult(scenario);
    }

    internal static async Task<TrainOptions> GetDefaultTrainOptionsAsync(MLScenarioTypes type, string dataFilePath)
    {
        if (!File.Exists(dataFilePath))
        {
            throw new FileNotFoundException($"File {dataFilePath} not found.");
        }

        var trainOptions = new TrainOptions
        {
            AllowQuote = true,
            HasHeader = true,
            LabelCol = null,
            IgnoreCols = null,
            TrainTime = null
        };

        using var reader = new StreamReader(dataFilePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            DetectDelimiter = true
        });

        // Read the header to determine if it exists and get column names
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;

        if (headers != null && headers.Length > 0)
        {
            trainOptions.HasHeader = true;

            if (type == MLScenarioTypes.Recommendation)
            {
                trainOptions.ItemCol = headers.ElementAt(0);
                trainOptions.UserCol = headers.ElementAt(1);
                trainOptions.RatingCol = headers.ElementAt(2);
            }
            else
            {
                trainOptions.LabelCol = headers.Last();
            }
        }
        else
        {
            trainOptions.HasHeader = false;
        }

        // Check if quotes are used in the file
        string firstLine = await reader.ReadLineAsync() ?? string.Empty;
        trainOptions.AllowQuote = firstLine.Contains('"');

        // Set a default train time (e.g., 5 minutes)
        trainOptions.TrainTime = 300; // 5 minutes in seconds

        return trainOptions;
    }

    internal static async Task<MLModel> CreateMLModelAsync(string modelName, string modelPath)
    {
        var trainState = await TrainStateHandler.GetTryAsync(modelPath) 
            ?? throw new InvalidOperationException($"Failed Train, ModelName: {modelName}");

        var model = new MLModel
        {
            Name = modelName,
            TrainState = trainState,
            //TrainResult = trainResult,
        };

        return model;
    }

    internal static async Task<MLTrainAction> BuildDefaultActionAsync(string modelPath, TrainOptions? trainOptions = null)
    {
        var sceanrioPath = Path.GetDirectoryName(modelPath)!;
        var scenario = await GetMLScenarioAsync(sceanrioPath);

        if (string.IsNullOrEmpty(scenario.DataPath)) throw new InvalidOperationException($"DataPath is required., {scenario.Name}");
        trainOptions ??= await GetDefaultTrainOptionsAsync(scenario.Type, scenario.DataPath);

        var dataPath = scenario.DataPath;
        var dataFileName = Path.GetFileName(dataPath);
        var modelDataPath = Path.Combine(modelPath, dataFileName);
        Directory.CreateDirectory(modelPath);
        File.Copy(scenario.DataPath, modelDataPath, true);

        var action = new MLTrainAction
        {
            Type = scenario.Type,
            Options = trainOptions,
            DataPath = modelDataPath
        };
        return action;
    }
}