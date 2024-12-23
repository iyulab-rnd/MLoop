// src/App.tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import '@shoelace-style/shoelace/dist/themes/light.css';
import { setBasePath } from '@shoelace-style/shoelace';
import { ScenariosPage } from './pages/ScenariosPage';
import { NewScenarioPage } from './pages/NewScenarioPage';
import { EditScenarioPage } from './pages/EditScenarioPage';
import { ScenarioLayout } from './components/layouts/ScenarioLayout';
import { ScenarioOverview } from './pages/ScenarioOverview';
import { ScenarioDataPage } from './pages/ScenarioDataPage';
import { ScenarioJobsPage } from './pages/ScenarioJobsPage';
import { ScenarioModelsPage } from './pages/ScenarioModelsPage';
import { ScenarioWorkflowsPage } from './pages/ScenarioWorkflowsPage';

setBasePath('https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.19.1/cdn/');

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-50">
        <main className="container mx-auto">
          <Routes>
            <Route path="/" element={<Navigate to="/scenarios" replace />} />
            <Route path="/scenarios" element={<ScenariosPage />} />
            <Route path="/scenarios/new" element={<NewScenarioPage />} />
            <Route path="/scenarios/:scenarioId/edit" element={<EditScenarioPage />} />
            <Route path="/scenarios/:scenarioId" element={<ScenarioLayout />}>
              <Route index element={<ScenarioOverview />} />
              <Route path="data" element={<ScenarioDataPage />} />
              <Route path="jobs" element={<ScenarioJobsPage />} />
              <Route path="models" element={<ScenarioModelsPage />} />
              <Route path="workflows" element={<ScenarioWorkflowsPage />} />
            </Route>
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;