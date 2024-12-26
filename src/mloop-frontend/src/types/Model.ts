export interface Model {
  modelId: string;
  mlType: string;
  command: string;
  arguments: Record<string, any>;
  metrics: Record<string, number>;
  createdAt: string;
}
