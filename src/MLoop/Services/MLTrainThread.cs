using MLoop.Models;
using MLoop.SystemText.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace MLoop.Services
{
    internal class MLTrainThread
    {
        private readonly BlockingCollection<TrainRequest> queue;

        /// <summary>
        /// 현재 작업 중인 모델의 키입니다.
        /// </summary>
        public string? WorkingModelKey { get; private set; }

        public MLTrainThread(BlockingCollection<TrainRequest> queue)
        {
            this.queue = queue;
        }

        public void Run()
        {
            foreach (var request in queue.GetConsumingEnumerable())
            {
                ProcessRequest(request).Wait();
            }
        }

        private async Task ProcessRequest(TrainRequest request)
        {
            this.WorkingModelKey = request.Key;

            Console.WriteLine($"Processing request {request} started.");

            var mlOptions = request.Options;
            var scenario = GetScenario(mlOptions.Scenario);
            var dir = Path.GetDirectoryName(request.DataPath)!;
            var logFilePath = Path.Combine(dir, "console.log");

            if (File.Exists(logFilePath)) File.Delete(logFilePath); // 기존 로그 파일 삭제

            try
            {
                await TrainStateHandler.BeginTrainAsync(dir);

                var options = new Dictionary<string, string>
                {
                    ["--dataset"] = request.DataPath,
                    ["--output"] = $"{dir}",
                    //["--log-file-path"] = $"{dir}/train.log",
                    ["--name"] = "Model",
                };

                if (request.TestPath != null) options["--validation-dataset"] = request.TestPath;
                if (mlOptions.HasHeader) options["--has-header"] = "true";
                if (mlOptions.AllowQuote) options["--allow-quote"] = "true";
                if (!string.IsNullOrEmpty(mlOptions.LabelCol)) options["--label-col"] = mlOptions.LabelCol;
                if (!string.IsNullOrEmpty(mlOptions.IgnoreCols)) options["--ignore-cols"] = mlOptions.IgnoreCols;
                if (mlOptions.TrainTime != null) options["--train-time"] = $"{mlOptions.TrainTime}";

                string arguments = string.Empty;

                foreach (var option in options)
                {
                    arguments += $@" {option.Key} ""{option.Value}""";
                }

                var currentPath = Directory.GetCurrentDirectory();

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = currentPath,
                        FileName = "mlnet",
                        Arguments = $"{scenario} {arguments}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                using var logFileWriter = new StreamWriter(logFilePath, append: true);
                var outputTask = LogOutputAsync(process.StandardOutput, logFileWriter);
                var errorTask = LogErrorAsync(process.StandardError, logFileWriter);

                await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync());

                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    await TrainStateHandler.OnErrorAsync(dir, error);

                    throw new Exception($"Training process failed: {error}");
                }
                else
                {
                    await TrainStateHandler.CompletedTrainAsync(dir);

                    await SaveResultFileAsync(dir);
                }

                Console.WriteLine($"Processing request {request} completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request {request}: {ex.Message}");

                await TrainStateHandler.OnErrorAsync(dir, ex.Message);
            }

            this.WorkingModelKey = null;
        }

        private async Task SaveResultFileAsync(string dir)
        {
            var filePath = Path.Combine(dir, "result.json");
            var result = new
            {
                Trainer = "LbfgsMaximumEntropyMulti",
                MacroAccuracy = 0.8494,
                Duration = 0.3490
            };

            var json = JsonHelper.Serialize(result);
            await File.WriteAllTextAsync(filePath, json);
        }

        private static async Task<string> LogOutputAsync(StreamReader reader, StreamWriter writer)
        {
            var output = new StringBuilder();
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (line != null)
                {
                    await writer.WriteLineAsync(line);
                    await writer.FlushAsync();
                    output.AppendLine(line);
                }
            }
            return output.ToString();
        }

        private static async Task<string> LogErrorAsync(StreamReader reader, StreamWriter writer)
        {
            var error = new StringBuilder();
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (line != null)
                {
                    await writer.WriteLineAsync(line);
                    await writer.FlushAsync();
                    error.AppendLine(line);
                }
            }
            return error.ToString();
        }

        private static string GetScenario(ModelScenarios scenario)
        {
            return scenario switch
            {
                ModelScenarios.Classification => "classification",
                ModelScenarios.Regression => "regression",
                ModelScenarios.Forecasting => "forecasting",
                ModelScenarios.Recommendation => "recommendation",
                ModelScenarios.ImageClassification => "image-classification",
                ModelScenarios.ObjectDetection => "object-detection",
                ModelScenarios.TextClassification => "text-classification",
                _ => throw new Exception("Invalid scenario"),
            };
        }
    }
}
