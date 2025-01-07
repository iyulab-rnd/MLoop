import { Scenario } from '../../../types/Scenario';

interface ScenarioOverviewProps {
  scenario: Scenario;
}

export const ScenarioOverview: React.FC<ScenarioOverviewProps> = ({ scenario }) => {
  return (
    <div className="p-6">
      <div className="mb-6">
        <h2 className="text-xl font-semibold text-gray-800 mb-2">ML Type</h2>
        <p className="text-gray-600">{scenario.mlType}</p>
      </div>

      <div className="mb-6">
        <h2 className="text-xl font-semibold text-gray-800 mb-2">Description</h2>
        <p className="text-gray-600 whitespace-pre-wrap">{scenario.description}</p>
      </div>
    </div>
  );
};
