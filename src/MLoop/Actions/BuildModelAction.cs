using MLoop.Contracts;

namespace MLoop.Actions
{
    public class BuildModelAction
    {
        public required string InputPath { get; set; }
        public required string OutputPath { get; set; }
    }

    public class BuildModelActionResult
    {
        public bool Success { get; internal set; }
    }

    public class BuildModelActionExecutor
    {
        private readonly IMLFileProvider _fileProvider;

        public BuildModelActionExecutor(IMLFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        public async Task<BuildModelActionResult> ExecuteAsync(BuildModelAction action, CancellationToken token)
        {
            var inputPath = action.InputPath;
            var outputPath = action.OutputPath;

            await Task.Delay(Random.Shared.Next(1, 10), token);

            var content = await _fileProvider.ReadFileAsync(inputPath, token);
            await _fileProvider.WriteFileAsync(outputPath, content, token);

            return new BuildModelActionResult
            {
                Success = true
            };
        }
    }
}