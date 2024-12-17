using MLoop.Worker.Services;
using MLoop.Storages;
using MLoop.Services;

var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "MLoop.Api", "storage");
var storage = new LocalFileStorage(basePath);
var runner = new MlnetCliRunner();
var scenarioManager = new ScenarioManager(storage);
var jobManager = new JobManager(storage);

var trainHandler = new TrainJobHandler(storage, runner, scenarioManager);
var predictHandler = new PredictJobHandler(storage, runner, scenarioManager);
var jobQueue = new LocalJobQueue(storage);

Console.WriteLine("Worker started. Press CTRL+C to stop.");
while (true)
{
    var job = jobQueue.DequeueJob();
    if (job == null)
    {
        await Task.Delay(2000);
        continue;
    }

    Console.WriteLine($"Processing Job: {job.JobId}, Type: {job.JobType}, Scenario: {job.ScenarioId}");

    job.Status = "Running";
    jobManager.SaveJobStatus(job);

    try
    {
        if (job.JobType == "Train")
        {
            trainHandler.Handle(job.ScenarioId);
            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;
            jobManager.SaveJobStatus(job);
            Console.WriteLine($"Train job {job.JobId} completed.");
        }
        else if (job.JobType == "Predict")
        {
            predictHandler.Handle(job.ScenarioId, job.JobId);
            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;
            jobManager.SaveJobStatus(job);
            Console.WriteLine($"Predict job {job.JobId} completed.");
        }
        else
        {
            Console.WriteLine($"Unknown job type: {job.JobType}");
            job.Status = "Failed";
            job.CompletedAt = DateTime.UtcNow;
            jobManager.SaveJobStatus(job);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Job {job.JobId} failed: {ex.Message}");
        job.Status = "Failed";
        job.CompletedAt = DateTime.UtcNow;
        jobManager.SaveJobStatus(job);
    }
}
