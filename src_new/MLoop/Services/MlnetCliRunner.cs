using System.Diagnostics;

namespace MLoop.Services;

public class MlnetCliRunner : IMlnetRunner
{
    private const string LabelColumn = "label";
    private const int TrainTimeSeconds = 30; // 예시 고정값

    // TrainModel에 scenarioCommand 추가
    public void TrainModel(string dataPath, string outputModelPath, string scenarioCommand)
    {
        var scenarioDir = Path.GetDirectoryName(outputModelPath);
        if (scenarioDir == null)
            throw new InvalidOperationException("Invalid output model path directory");

        Directory.CreateDirectory(scenarioDir);

        // scenarioCommand를 사용 (classification, regression, 등)
        var args = $" {scenarioCommand} --dataset \"{dataPath}\" --label-col \"{LabelColumn}\" --has-header true -o \"{scenarioDir}\" --train-time {TrainTimeSeconds} --verbosity m";

        RunProcess("mlnet", args);

        var generatedModelFile = Path.Combine(scenarioDir, "MLModel.zip");
        if (!File.Exists(generatedModelFile))
        {
            throw new FileNotFoundException("MLModel.zip not found in output directory");
        }

        File.Copy(generatedModelFile, outputModelPath, true);
    }

    public void Predict(string modelPath, string inputDataPath, string outputResultPath)
    {
        var outputDir = Path.GetDirectoryName(outputResultPath) ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDir);

        var args = $"--model \"{modelPath}\" --input \"{inputDataPath}\" --has-header true --output \"{outputResultPath}\"";
        RunProcess("mlnet-predict", args);

        if (!File.Exists(outputResultPath))
        {
            throw new FileNotFoundException("Prediction result file not generated.");
        }
    }

    private void RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException($"Failed to start process {fileName}");

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            string errorOutput = process.StandardError.ReadToEnd();
            throw new Exception($"CLI command failed with exit code {process.ExitCode}: {errorOutput}");
        }
    }
}
