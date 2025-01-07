import { useOutletContext } from "react-router-dom";
import { Scenario } from "../../types/Scenario";
import { LoadingSpinner } from "../../components/common/LoadingSpinner";
import { PredictionsHeader } from "./components/PredictionsHeader";
import { EmptyPredictionsState } from "./components/EmptyPredictionsState";
import { PredictionsTable } from "./components/PredictionsTable";
import { usePredictions } from "./hooks/usePredictions";

type ScenarioContextType = {
  scenario: Scenario;
};

export const PredictionsPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  const {
    predictions,
    loading,
    cleanupInProgress,
    handleCleanup
  } = usePredictions(scenario.scenarioId);

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <div className="p-6">
      <PredictionsHeader
        hasData={predictions.length > 0}
        onCleanup={handleCleanup}
        cleanupInProgress={cleanupInProgress}
      />

      {predictions.length === 0 ? (
        <EmptyPredictionsState scenarioName={scenario.name} />
      ) : (
        <PredictionsTable
          predictions={predictions}
          scenarioId={scenario.scenarioId}
        />
      )}
    </div>
  );
};