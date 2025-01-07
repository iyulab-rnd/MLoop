import { useOutletContext } from "react-router-dom";
import { Scenario } from "../../types/Scenario";
import { LoadingSpinner } from "../../components/common/LoadingSpinner";
import { ModelListHeader } from "./components/ModelListHeader";
import { EmptyModelState } from "./components/EmptyModelState";
import { ModelTable } from "./components/ModelTable";
import { useModels } from "./hooks/useModels";

type ScenarioContextType = {
  scenario: Scenario;
};

export const MLModelListPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  const {
    models,
    loading,
    cleanupInProgress,
    trainingInProgress,
    handleStartTraining,
    handleCleanup,
    handleDeleteModel,
  } = useModels(scenario.scenarioId);

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <div className="p-6">
      <ModelListHeader
        modelCount={models.length}
        onStartTraining={handleStartTraining}
        onCleanup={handleCleanup}
        trainingInProgress={trainingInProgress}
        cleanupInProgress={cleanupInProgress}
      />

      {models.length === 0 ? (
        <EmptyModelState scenarioName={scenario.name} />
      ) : (
        <ModelTable
          models={models}
          scenarioId={scenario.scenarioId}
          onDeleteModel={handleDeleteModel}
        />
      )}
    </div>
  );
};