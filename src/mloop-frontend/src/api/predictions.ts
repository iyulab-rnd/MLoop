import { api } from './api';
import { 
  Prediction, 
  PredictionFile, 
  PredictionResult, 
  PredictionStatus,
  PredictionCleanupResult 
} from '../types/Prediction';

interface EmptyRequest {
  // 빈 요청을 위한 인터페이스
  [key: string]: never;
}

export const predictionsApi = {
  // 예측 목록 조회
  list: async (scenarioId: string): Promise<Prediction[]> => {
    return api.get<Prediction[]>(`/api/scenarios/${scenarioId}/predictions`);
  },

  // 예측 실행
  predict: async (scenarioId: string, input: string): Promise<{ predictionId: string }> => {
    return api.post<{ predictionId: string }, string>(
      `/api/scenarios/${scenarioId}/predict`,
      input,
      {
        headers: {
          'Content-Type': 'text/tab-separated-values',
        },
      }
    );
  },

  // 예측 결과 조회
  getResult: async (scenarioId: string, predictionId: string): Promise<PredictionResult | PredictionStatus> => {
    return api.get<PredictionResult | PredictionStatus>(
      `/api/scenarios/${scenarioId}/predictions/${predictionId}`
    );
  },

  // 예측 파일 목록 조회
  listFiles: async (scenarioId: string, predictionId: string): Promise<PredictionFile[]> => {
    return api.get<PredictionFile[]>(
      `/api/scenarios/${scenarioId}/predictions/${predictionId}/files`
    );
  },

  // 예측 클린업
  cleanup: async (scenarioId: string): Promise<PredictionCleanupResult> => {
    return api.post<PredictionCleanupResult, EmptyRequest>(
      `/api/scenarios/${scenarioId}/predictions/cleanup`,
      {}
    );
  }
};