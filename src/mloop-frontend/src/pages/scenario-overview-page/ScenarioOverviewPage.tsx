import { useOutletContext } from "react-router-dom";
import { LoadingSpinner } from "../../components/common/LoadingSpinner";
import { MLTypeSection } from "./components/MLTypeSection";
import { TagSection } from "./components/TagSection";
import { OverviewSection } from "./components/OverviewSection";
import { MetricsSection } from "./components/MetricsSection";
import { useScenarioOverview } from "./hooks/useScenarioOverview";
import { Scenario } from "../../types/Scenario";

type ScenarioContextType = {
  scenario: Scenario;
};

export const ScenarioOverviewPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  const { loading, error } = useScenarioOverview(scenario.scenarioId);

  if (loading) {
    return <LoadingSpinner />;
  }

  if (error) {
    return (
      <div className="p-6">
        <div className="p-4 bg-red-50 text-red-600 rounded-lg">{error}</div>
      </div>
    );
  }

  return (
    <div className="p-6">
      <MetricsSection scenario={scenario} />

      <MLTypeSection mlType={scenario.mlType} />

      <TagSection tags={scenario.tags} />

      {scenario.description && (
        <OverviewSection title="Description" content={scenario.description} />
      )}
    </div>
  );
};
