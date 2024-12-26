export interface ModelArguments {
  [key: string]: string | number | boolean | null;
}

export interface Model {
  modelId: string;
  mlType: string;
  command: string;
  arguments: ModelArguments;
  metrics: Record<string, number>;
  createdAt: string;
}