import { useNavigate } from "react-router-dom";
import { useScenarios } from "../hooks/scenarios/useScenarios";
import { ScenarioCard } from "../components/scenarios/ScenarioCard";
import { ScenarioSearch } from "../components/scenarios/ScenarioSearch";
import { AddScenarioCard } from "../components/scenarios/AddScenarioCard";
import { Scenario } from "../types/Scenario";

export const ScenarioListPage = () => {
  const { scenarios, searchTerm, setSearchTerm, loading, error } =
    useScenarios();
  const navigate = useNavigate();
  
  const handleScenarioClick = (scenario: Scenario) => {
    navigate(`/scenarios/${scenario.scenarioId}`);
  };

  if (error) {
    return (
      <div className="max-w-[1200px] mx-auto px-8 py-12">
        <div className="p-4 bg-red-50 rounded-lg text-red-600 text-center">
          Error loading scenarios: {error.message}
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-[1200px] mx-auto px-8 py-12">
      <div className="mb-12">
        <h1 className="text-3xl font-bold text-gray-900 mb-8">
          Select Scenario
        </h1>
        <ScenarioSearch value={searchTerm} onChange={setSearchTerm} />
      </div>

      {loading ? (
        <div className="flex items-center justify-center h-[400px]">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-8 auto-rows-fr h-full">
          {scenarios.map((scenario) => (
            <ScenarioCard
              key={scenario.scenarioId}
              scenario={scenario}
              onClick={handleScenarioClick}
            />
          ))}
          <AddScenarioCard />
        </div>
      )}
    </div>
  );
};