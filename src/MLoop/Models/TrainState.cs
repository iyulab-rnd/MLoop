using MLoop.Utils;
using System.Text.Json;

namespace MLoop.Models
{
    public enum TrainStatus
    {
        Pending,
        Training,
        Completed,
        Error
    }

    public class TrainState
    {
        public required string Key { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? TrainingBegin { get; set; }
        public DateTime? TrainingCompleted { get; set; }
        public string? Error { get; set; }

        public TrainStatus Status
        {
            get
            {
                if (this.Error != null) return TrainStatus.Error;
                else if (this.TrainingCompleted != null) return TrainStatus.Completed;
                else if (this.TrainingBegin != null) return TrainStatus.Training;
                else return TrainStatus.Pending;
            }
        }
    }

    public static class TrainStateHandler
    {
        private static async Task<TrainState?> GetTryAsync(string basePath)
        {
            if (!Directory.Exists(basePath)) throw new Exception("Invalid SID");

            var modelJsonFile = Path.Combine(basePath, "state.json");
            if (!File.Exists(modelJsonFile)) return null;

            var modelJson = await File.ReadAllTextAsync(modelJsonFile);
            var model = JsonHelper.Deserialize<TrainState>(modelJson);
            return model;
        }

        private static async Task<TrainState> GetAsync(string basePath)
        {
            var model = await GetTryAsync(basePath) ?? throw new Exception("Invalid state.json");
            var modelKey = Path.GetFileName(basePath);
            model.Key = modelKey;

            var errorPath = Path.Combine(basePath, "error.txt");
            if (File.Exists(errorPath))
            {
                var error = await File.ReadAllTextAsync(errorPath);
                model.Error = error;
            }

            return model;
        }

        private static async Task SaveAsync(this TrainState model, string dir)
        {
            var path = Path.Combine(dir, "state.json");
            var modelJson = JsonHelper.Serialize(model);
            await File.WriteAllTextAsync(path, modelJson);
        }
        
        internal static async Task BeginTrainAsync(string dir)
        {
            var state = await GetAsync(dir);
            state.TrainingBegin = DateTime.UtcNow;
            await state.SaveAsync(dir);
        }

        internal static async Task CompletedTrainAsync(string dir)
        {
            var state = await GetAsync(dir);
            state.Error = null;
            state.TrainingCompleted = DateTime.UtcNow;
            await state.SaveAsync(dir);
        }

        internal static async Task OnErrorAsync(string dir, string error)
        {
            var state = await GetAsync(dir);
            state.Error = error;
            await state.SaveAsync(dir);
        }

        internal static async Task<string> ResolveKey(string dir)
        {
            var state = await GetTryAsync(dir);
            if (state != null) return state.Key;

            var key = RandomHelper.RandomString(8);
            state = new TrainState { Key = key };
            await state.SaveAsync(dir);

            return key;
        }
    }
}
