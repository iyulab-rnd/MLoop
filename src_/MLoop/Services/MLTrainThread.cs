using MLoop.Models;
using MLoop.SystemText.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml;

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
            var scenarioType = GetScenarioType(request.Type);
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

                if (!string.IsNullOrEmpty(mlOptions.UserCol)) options["--user-col"] = mlOptions.UserCol;
                if (!string.IsNullOrEmpty(mlOptions.ItemCol)) options["--item-col"] = mlOptions.ItemCol;
                if (!string.IsNullOrEmpty(mlOptions.RatingCol)) options["--rating-col"] = mlOptions.RatingCol;

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
                        Arguments = $"{scenarioType} {arguments}",
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

        private static async Task SaveResultFileAsync(string dir)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            var resultFilePath = Path.Combine(dir, "result.json");
            var mbconfigFilePath = Path.Combine(dir, "Model", "Model.mbconfig");

            if (File.Exists(mbconfigFilePath))
            {
                try
                {
                    // Model.mbconfig 파일 읽기
                    var mbconfigJson = await File.ReadAllTextAsync(mbconfigFilePath);
                    using JsonDocument doc = JsonDocument.Parse(mbconfigJson);

                    // RunHistory.Trials 배열 가져오기
                    if (doc.RootElement.TryGetProperty("RunHistory", out JsonElement runHistory) &&
                        runHistory.TryGetProperty("Trials", out JsonElement trials) &&
                        trials.ValueKind == JsonValueKind.Array &&
                        trials.GetArrayLength() > 0)
                    {
                        // 가장 높은 Score를 가진 Trial 찾기
                        var bestTrial = trials.EnumerateArray()
                            .OrderByDescending(t => t.GetProperty("Score").GetDouble())
                            .FirstOrDefault();

                        if (bestTrial.ValueKind != JsonValueKind.Undefined)
                        {
                            // 익명 클래스로 결과 생성
                            var result = new
                            {
                                TrainerName = bestTrial.GetProperty("TrainerName").GetString(),
                                Score = bestTrial.GetProperty("Score").GetDouble()
                            };

                            // JSON 직렬화 및 파일 저장
                            var json = JsonHelper.Serialize(result);
                            await File.WriteAllTextAsync(resultFilePath, json);
                        }
                        else
                        {
                            throw new Exception("Trials 배열에 유효한 항목이 없습니다.");
                        }
                    }
                    else
                    {
                        throw new Exception("RunHistory.Trials 섹션을 찾을 수 없습니다.");
                    }
                }
                catch (Exception)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    var result = new { };
                    var json = JsonHelper.Serialize(result);
                    await File.WriteAllTextAsync(resultFilePath, json);
                }
            }
            else
            {   
                var result = new { };
                var json = JsonHelper.Serialize(result);
                await File.WriteAllTextAsync(resultFilePath, json);
            }
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

        private static string GetScenarioType(MLScenarioTypes scenario)
        {
            return scenario switch
            {
                MLScenarioTypes.Classification => "classification",
                MLScenarioTypes.Regression => "regression",
                MLScenarioTypes.Forecasting => "forecasting",
                MLScenarioTypes.Recommendation => "recommendation",
                MLScenarioTypes.ImageClassification => "image-classification",
                MLScenarioTypes.ObjectDetection => "object-detection",
                MLScenarioTypes.TextClassification => "text-classification",
                _ => throw new Exception("Invalid scenario"),
            };
        }
    }
}
