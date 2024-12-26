export interface Prediction {
    predictionId: string;
    modelId: string;
    status: string;
    createdAt: string;
    completedAt?: string;
    hasResult: boolean;
    inputFile: string;
  }
  
  export interface PredictionCleanupResult {
    message: string;
    cleanedCount: number;
  }
  
  export interface PredictionFile {
    name: string;
    path: string;
    size: number;
    lastModified: string;
  }
  
  export interface PredictionResult {
    Top1: string;
    Top1Score: number;
    Top2: string;
    Top2Score: number;
    Top3: string;
    Top3Score: number;
  }
  
  export interface PredictionStatus {
    status: "processing" | "completed";
    jobStatus?: string;
    message?: string;
    modelId?: string;
  }