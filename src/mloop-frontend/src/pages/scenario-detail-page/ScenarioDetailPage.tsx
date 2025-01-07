import { useParams, useLocation, Routes, Route } from "react-router-dom";
import { LoadingSpinner } from "../../components/common/LoadingSpinner";
import { ContentWrapper } from "./components/ContentWrapper";
import { NavigationHeader } from "./components/NavigationHeader";
import { ScenarioHeader } from "./components/ScenarioHeader";
import { TabNavigation } from "./components/TabNavigation";
import { ScenarioOverview } from "./components/ScenarioOverview";
import { useScenarioDetail } from "./hooks/useScenarioDetail";
import { DataPage } from "../data-page/DataPage";
import { JobListPage } from "../job-list-page/JobListPage";
import { MLModelListPage } from "../model-list-page/MLModelListPage";
import { ScenarioWorkflowsPage } from "../scenario-workflows-page/ScenarioWorkflowsPage";

export const ScenarioDetailPage = () => {
  const { scenarioId } = useParams();
  const location = useLocation();
  const { scenario, bestModel, loading, error, handleEdit, handlePredict } =
    useScenarioDetail(scenarioId);

  if (loading) {
    return <LoadingSpinner />;
  }

  if (!scenario) {
    return (
      <ContentWrapper error="Scenario not found">
        <NavigationHeader />
      </ContentWrapper>
    );
  }

  const currentPath = location.pathname;
  const baseUrl = `/scenarios/${scenarioId}`;

  return (
    <ContentWrapper error={error}>
      <NavigationHeader />

      <div className="bg-white rounded-lg shadow">
        <div className="p-6 border-b border-gray-200">
          <ScenarioHeader
            scenario={scenario}
            bestModel={bestModel}
            onEdit={handleEdit}
            onPredict={handlePredict}
          />

          <TabNavigation currentPath={currentPath} baseUrl={baseUrl} />
        </div>

        <Routes>
          <Route index element={<ScenarioOverview scenario={scenario} />} />
          <Route path="data" element={<DataPage />} />
          <Route path="jobs" element={<JobListPage />} />
          <Route path="models" element={<MLModelListPage />} />
          <Route path="workflows" element={<ScenarioWorkflowsPage />} />
        </Routes>
      </div>
    </ContentWrapper>
  );
};
