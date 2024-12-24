import { useOutletContext } from 'react-router-dom';
import { Scenario } from '../types/scenarios';

type ScenarioContextType = {
  scenario: Scenario;
};

export const ScenarioOverview = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  
  return (
    <div className="p-6">
      <div className="mb-6">
        <h2 className="text-lg text-gray-500 mb-2">ML Type</h2>
        <p className="text-gray-900 font-medium">{scenario.mlType}</p>
      </div>
      <div className="mb-6">
        <h2 className="text-lg text-gray-500 mb-2">Description</h2>
        <p className="text-gray-900 font-medium whitespace-pre-wrap">{scenario.description}</p>
      </div>
    </div>
  );
};