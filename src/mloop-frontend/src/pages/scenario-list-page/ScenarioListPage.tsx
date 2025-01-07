import { FC } from 'react';
import { useNavigate } from 'react-router-dom';
import { useScenarios } from '../../hooks/scenarios/useScenarios';
import { ScenarioGrid } from './components/ScenarioGrid';
import { ErrorDisplay } from '../../components/common/ErrorDisplay';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { Scenario } from '../../types/Scenario';
import { ScenarioSearch } from './components/ScenarioSearch';

export const ScenarioListPage: FC = () => {
  const { scenarios, searchTerm, setSearchTerm, loading, error } = useScenarios();
  const navigate = useNavigate();
  
  const handleScenarioClick = (scenario: Scenario) => {
    navigate(`/scenarios/${scenario.scenarioId}`);
  };

  return (
    <div className="max-w-[1200px] mx-auto px-8 py-12">
      <div className="mb-12">
        <h1 className="text-3xl font-bold text-gray-900 mb-8">
          Select Scenario
        </h1>
        <ScenarioSearch value={searchTerm} onChange={setSearchTerm} />
      </div>

      {error ? (
        <ErrorDisplay message={error.message} />
      ) : loading ? (
        <LoadingSpinner />
      ) : (
        <ScenarioGrid 
          scenarios={scenarios}
          onScenarioClick={handleScenarioClick}
        />
      )}
    </div>
  );
};