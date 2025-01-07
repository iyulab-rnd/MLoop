import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import '@shoelace-style/shoelace/dist/themes/light.css';
import { setBasePath } from '@shoelace-style/shoelace';
import { NotificationProvider } from './contexts/NotificationProvider'
import { ErrorBoundary } from './components/common/ErrorBoundary';
import { ScenarioListPage } from './pages/ScenarioListPage';
import { NewScenarioPage } from './pages/NewScenarioPage';
import { EditScenarioPage } from './pages/EditScenarioPage';
import { ScenarioLayout } from './components/layouts/ScenarioLayout';
import { ScenarioOverview } from './pages/ScenarioOverview';
import { DataPage } from './pages/DataPage';
import { JobListPage } from './pages/JobListPage';
import { JobDetailPage } from './pages/JobDetailPage';
import { ScenarioWorkflowsPage } from './pages/ScenarioWorkflowsPage';
import { MLModelListPage } from './pages/MLModelListPage';
import { MLModelDetailPage } from './pages/MLModelDetailPage';
import { PredictionsPage } from './pages/PredictionsPage';
import { PredictionDetailPage } from './pages/PredictionDetailPage';
import { PredictPage } from './pages/predict-page/PredictPage';

setBasePath('https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.19.1/cdn/');

function App() {
  return (
    <ErrorBoundary>
      <NotificationProvider>
        <BrowserRouter>
          <div className="min-h-screen bg-gray-50">
            <main className="container mx-auto">
              <Routes>
                <Route path="/" element={<Navigate to="/scenarios" replace />} />
                <Route path="/scenarios" element={<ScenarioListPage />} />
                <Route path="/scenarios/new" element={<NewScenarioPage />} />
                <Route path="/scenarios/:scenarioId/edit" element={<EditScenarioPage />} />
                <Route path="/scenarios/:scenarioId" element={<ScenarioLayout />}>
                  <Route index element={<ScenarioOverview />} />
                  <Route path="data" element={<DataPage />} />
                  <Route path="jobs" element={<JobListPage />} />
                  <Route path="jobs/:jobId" element={<JobDetailPage />} />
                  <Route path="models" element={<MLModelListPage />} />
                  <Route path="models/:modelId" element={<MLModelDetailPage />} />
                  <Route path="workflows" element={<ScenarioWorkflowsPage />} />
                  <Route path="predictions" element={<PredictionsPage />} />
                  <Route path="predictions/:predictionId" element={<PredictionDetailPage />} />
                </Route>
                <Route path="/scenarios/:scenarioId/models/:modelId/predict" element={<PredictPage />} />
              </Routes>
            </main>
          </div>
        </BrowserRouter>
      </NotificationProvider>
    </ErrorBoundary>
  );
}

export default App;