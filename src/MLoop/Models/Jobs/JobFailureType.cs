﻿namespace MLoop.Models.Jobs;

public enum JobFailureType
{
    None,
    Timeout,
    WorkerCrash,
    ValidationError,
    ProcessError,
    FileNotFound,
    ConfigurationError,
    UnknownError,
    ResourceExhausted,
    TrainingError,
    DataProcessingError,
}