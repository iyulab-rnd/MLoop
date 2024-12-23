export interface Scenario {
  scenarioId: string;
  name: string;
  description: string;
  tags: string[];
  mlType: string;
  createdAt: string;
}

export interface ScenarioSearchParams {
  searchTerm?: string;
}
