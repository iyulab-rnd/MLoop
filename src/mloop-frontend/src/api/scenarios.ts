import { Scenario, ScenarioSearchParams } from '../types/scenarios';

export const fetchScenarios = async (params?: ScenarioSearchParams): Promise<Scenario[]> => {
  try {
    let url = '/api/scenarios';
    
    if (params?.searchTerm) {
      // OData 필터 적용
      const filter = encodeURIComponent(`contains(tolower(name),tolower('${params.searchTerm}')) or tags/any(t: contains(tolower(t),tolower('${params.searchTerm}')))`);
      url += `?$filter=${filter}`;
    }

    const response = await fetch(url);
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to fetch scenarios: ${response.status} ${errorText}`);
    }

    const contentType = response.headers.get('content-type');
    if (!contentType || !contentType.includes('application/json')) {
      throw new Error(`Invalid content type: ${contentType}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Error in fetchScenarios:', error);
    throw error instanceof Error ? error : new Error('Failed to fetch scenarios');
  }
};