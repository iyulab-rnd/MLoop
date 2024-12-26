export interface JobStatusHistory {
    status: string;
    timestamp: string;
    workerId: string;
    message: string;
  }
  
  export interface Job {
    jobId: string;
    scenarioId: string;
    status: string;
    workerId: string;
    createdAt: string;
    startedAt: string;
    jobType: string;
    statusHistory: JobStatusHistory[];
    failureType: string;
    modelId: string;
  }
  