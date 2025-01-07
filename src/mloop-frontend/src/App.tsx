import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import "@shoelace-style/shoelace/dist/themes/light.css";
import { setBasePath } from "@shoelace-style/shoelace";
import { NotificationProvider } from "./contexts/NotificationProvider";
import { ErrorBoundary } from "./components/common/ErrorBoundary";
import { ScenarioLayout } from "./components/layouts/ScenarioLayout";
import { PredictPage } from "./pages/predict-page/PredictPage";
import { DataPage } from "./pages/data-page/DataPage";
import { EditScenarioPage } from "./pages/edit-scenario-page/EditScenarioPage";
import { JobDetailPage } from "./pages/job-detail-page/JobDetailPage";
import { JobListPage } from "./pages/job-list-page/JobListPage";
import { MLModelDetailPage } from "./pages/model-detail-page/MLModelDetailPage";
import { MLModelListPage } from "./pages/model-list-page/MLModelListPage";
import { NewScenarioPage } from "./pages/new-scenario-page/NewScenarioPage";
import { PredictionDetailPage } from "./pages/prediction-detail-page/PredictionDetailPage";
import { PredictionsPage } from "./pages/predictions-page/PredictionsPage";
import { ScenarioListPage } from "./pages/scenario-list-page/ScenarioListPage";
import { ScenarioWorkflowsPage } from "./pages/scenario-workflows-page/ScenarioWorkflowsPage";
import { ScenarioOverviewPage } from "./pages/scenario-overview-page/ScenarioOverviewPage";

setBasePath(
  "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.19.1/cdn/"
);

function App() {
  return (
    <ErrorBoundary>
      <NotificationProvider>
        <BrowserRouter>
          <div className="min-h-screen bg-gray-50">
            <main className="container mx-auto">
              <Routes>
                <Route
                  path="/"
                  element={<Navigate to="/scenarios" replace />}
                />
                <Route path="/scenarios" element={<ScenarioListPage />} />
                <Route path="/scenarios/new" element={<NewScenarioPage />} />
                <Route
                  path="/scenarios/:scenarioId/edit"
                  element={<EditScenarioPage />}
                />
                <Route
                  path="/scenarios/:scenarioId"
                  element={<ScenarioLayout />}
                >
                  <Route index element={<ScenarioOverviewPage />} />
                  <Route path="data" element={<DataPage />} />
                  <Route path="jobs" element={<JobListPage />} />
                  <Route path="jobs/:jobId" element={<JobDetailPage />} />
                  <Route path="models" element={<MLModelListPage />} />
                  <Route
                    path="models/:modelId"
                    element={<MLModelDetailPage />}
                  />
                  <Route path="workflows" element={<ScenarioWorkflowsPage />} />
                  <Route path="predictions" element={<PredictionsPage />} />
                  <Route
                    path="predictions/:predictionId"
                    element={<PredictionDetailPage />}
                  />
                </Route>
                <Route
                  path="/scenarios/:scenarioId/models/:modelId/predict"
                  element={<PredictPage />}
                />
              </Routes>
            </main>
          </div>
        </BrowserRouter>
      </NotificationProvider>
    </ErrorBoundary>
  );
}

export default App;
