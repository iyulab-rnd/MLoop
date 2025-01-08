import { FC } from 'react';
import { Scenario } from '../../../types/Scenario';
import { ScenarioCard } from './ScenarioCard';
import { AddScenarioCard } from './AddScenarioCard';

interface ScenarioGridProps {
  scenarios: Scenario[];
  onScenarioClick: (scenario: Scenario) => void;
}

export const ScenarioGrid: FC<ScenarioGridProps> = ({ scenarios, onScenarioClick }) => {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {scenarios.map((scenario) => (
        <ScenarioCard
          key={scenario.scenarioId}
          scenario={scenario}
          onClick={onScenarioClick}
        />
      ))}
      <div className="relative pb-[75%]">
        <AddScenarioCard className="absolute inset-0" />
      </div>
    </div>
  );
};

export default ScenarioGrid;