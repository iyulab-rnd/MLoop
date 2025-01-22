## Storage
```
/storage/scenarios/<scenarioId>/scenario.json # scenario metadata file
/storage/scenarios/<scenarioId>/data # train, test files
/storage/scenarios/<scenarioId>/workflows # Workflow Files
/storage/scenarios/<scenarioId>/workflows/train.yaml # Train Workflow File
/storage/scenarios/<scenarioId>/jobs # Job Files
/storage/scenarios/<scenarioId>/jobs/<jobid>.json # Job File
/storage/scenarios/<scenarioId>/jobs/<jobid>.log # Job Log File
/storage/scenarios/<scenarioId>/jobs/<jobid>_result.json # Job Result File
/storage/scenarios/<scenarioId>/models # Models
/storage/scenarios/<scenarioId>/models/<modelId> # Model Base
/storage/scenarios/<scenarioId>/models/<modelId>/train.log # Model Train Logs
/storage/scenarios/<scenarioId>/models/<modelId>/model.json # model metadata file
/storage/scenarios/<scenarioId>/models/<modelId>/metrics.json # model metrics file
/storage/scenarios/<scenarioId>/models/<modelId>/Model # Model Proj
/storage/scenarios/<scenarioId>/models/<modelId>/Model/Model.mlnet # MLNet Model File

/storage/scenarios/<scenarioId>/predictions/<jobId>/input.tsv # prediction request file (tsv | csv)
/storage/scenarios/<scenarioId>/predictions/<jobId>/input1.png...input100.png # many images
/storage/scenarios/<scenarioId>/predictions/<jobId>/result.csv # prediction result
```


## Project Structure
```
MLoop # 공통 프로젝트
MLoop.Worker # Worker 프로젝트
MLoop.API # API 프로젝트
```
