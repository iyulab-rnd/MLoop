import { DataFile, Scenario, ScenarioSearchParams, Job, Model } from "../types";
import { api } from "./api";

type EmptyRequest = Record<string, never>;

export const scenarioApi = {
  // 시나리오 목록 조회
  list: async (params?: ScenarioSearchParams): Promise<Scenario[]> => {
    let url = "/api/scenarios";
    if (params?.searchTerm) {
      const filter = encodeURIComponent(
        `contains(tolower(name),tolower('${params.searchTerm}')) or tags/any(t: contains(tolower(t),tolower('${params.searchTerm}')))`
      );
      url += `?$filter=${filter}`;
    }
    return api.get<Scenario[]>(url);
  },

  // 단일 시나리오 조회
  get: async (id: string): Promise<Scenario> => {
    return api.get<Scenario>(`/api/scenarios/${id}`);
  },

  // 시나리오 생성
  create: async (data: Partial<Scenario>): Promise<Scenario> => {
    return api.post<Scenario, Partial<Scenario>>("/api/scenarios", data);
  },

  // 시나리오 수정
  update: async (id: string, data: Partial<Scenario>): Promise<Scenario> => {
    return api.put<Scenario, Partial<Scenario>>(`/api/scenarios/${id}`, data);
  },

  // 시나리오 삭제
  delete: async (id: string): Promise<void> => {
    return api.delete<void>(`/api/scenarios/${id}`);
  },

  // 모델 트레이닝 시작
  startTraining: async (scenarioId: string): Promise<{ jobId: string }> => {
    return api.post<{ jobId: string }, EmptyRequest>(
      `/api/scenarios/${scenarioId}/train`,
      {}
    );
  },

  // 데이터 파일 멀티 업로드
  uploadFiles: async (scenarioId: string, files: File[]): Promise<void> => {
    const formData = new FormData();
    files.forEach((file) => {
      formData.append("files", file);
    });

    return api.post<void, FormData>(
      `/api/scenarios/${scenarioId}/data`,
      formData
    );
  },

  // 데이터 파일 목록 조회
  listFiles: async (scenarioId: string): Promise<DataFile[]> => {
    return api.get<DataFile[]>(`/api/scenarios/${scenarioId}/data`);
  },

  // 데이터 파일 삭제
  deleteFile: async (scenarioId: string, fileName: string): Promise<void> => {
    return api.delete<void>(`/api/scenarios/${scenarioId}/data/${fileName}`);
  },

  // 모델 목록 조회
  listModels: async (scenarioId: string): Promise<Model[]> => {
    return api.get<Model[]>(`/api/scenarios/${scenarioId}/models`);
  },

  // 모델 상세 조회
  getModel: async (scenarioId: string, modelId: string): Promise<Model> => {
    return api.get<Model>(`/api/scenarios/${scenarioId}/models/${modelId}`);
  },

  deleteModel: async (scenarioId: string, modelId: string): Promise<void> => {
    return api.delete<void>(`/api/scenarios/${scenarioId}/models/${modelId}`);
  },

  // 모델 클린업
  cleanupModels: async (scenarioId: string): Promise<{ message?: string }> => {
    return api.post<{ message?: string }, EmptyRequest>(
      `/api/scenarios/${scenarioId}/models/cleanup`,
      {}
    );
  },

  // 작업 목록 조회
  listJobs: async (scenarioId: string): Promise<Job[]> => {
    return api.get<Job[]>(`/api/scenarios/${scenarioId}/jobs`);
  },

  // 작업 상세 조회
  getJob: async (scenarioId: string, jobId: string): Promise<Job> => {
    return api.get<Job>(`/api/scenarios/${scenarioId}/jobs/${jobId}`);
  },

  // 작업 로그 조회
  getJobLogs: async (scenarioId: string, jobId: string): Promise<string> => {
    return api.get<string>(`/api/scenarios/${scenarioId}/jobs/${jobId}/logs`);
  },

  // 작업 취소
  cancelJob: async (scenarioId: string, jobId: string): Promise<void> => {
    return api.post<void, EmptyRequest>(
      `/api/scenarios/${scenarioId}/jobs/${jobId}/cancel`,
      {}
    );
  },

  // 작업 클린업
  cleanupJobs: async (scenarioId: string): Promise<{ message?: string }> => {
    return api.post<{ message?: string }, EmptyRequest>(
      `/api/scenarios/${scenarioId}/jobs/cleanup`,
      {}
    );
  },

  // 워크플로우 조회
  getWorkflow: async (
    scenarioId: string,
    type: "train" | "predict"
  ): Promise<string> => {
    return api.get<string>(`/api/scenarios/${scenarioId}/workflows/${type}`);
  },

  // 워크플로우 저장
  saveWorkflow: async (
    scenarioId: string,
    type: "train" | "predict",
    config: string
  ): Promise<void> => {
    return api.post<void, string>(
      `/api/scenarios/${scenarioId}/workflows/${type}`,
      config,
      {
        headers: {
          "Content-Type": "text/yaml",
        },
      }
    );
  },

  getModelLogs: async (
    scenarioId: string,
    modelId: string
  ): Promise<string> => {
    return api.get<string>(
      `/api/scenarios/${scenarioId}/models/${modelId}/logs/train`
    );
  },

  predict: async (
    scenarioId: string,
    input: string
  ): Promise<{ predictionId: string }> => {
    return api.post<{ predictionId: string }, string>(
      `/api/scenarios/${scenarioId}/predict`,
      input,
      {
        headers: {
          "Content-Type": "text/tab-separated-values",
        },
      }
    );
  },

  getPredictionResult: async (
    scenarioId: string,
    predictionId: string
  ): Promise<Record<string, string | number>> => {
    return api.get<Record<string, string | number>>(
      `/api/scenarios/${scenarioId}/predictions/${predictionId}`
    );
  },
};
