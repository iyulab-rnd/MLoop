import { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation, Link, Routes, Route } from 'react-router-dom';
import { SlButton, SlIcon, SlTag, SlAlert } from '@shoelace-style/shoelace/dist/react';
import { Scenario } from '../types/Scenario';
import { scenarioApi } from '../api/scenarios';
import { useNotification } from '../contexts/NotificationContext';
import { DataPage } from './DataPage';
import { JobListPage } from './JobListPage';
import { ScenarioWorkflowsPage } from './ScenarioWorkflowsPage';
import { MLModelListPage } from './MLModelListPage';

const TabItem = ({ to, current, children }: { to: string; current: boolean; children: React.ReactNode }) => (
  <Link
    to={to}
    className={`px-4 py-2 font-medium text-sm rounded-md transition-colors
      ${current 
        ? 'bg-white text-blue-600 shadow' 
        : 'text-gray-600 hover:text-gray-900 hover:bg-white/60'
      }`}
  >
    {children}
  </Link>
);

const ScenarioOverview = ({ scenario }: { scenario: Scenario }) => (
  <div className="p-6">
    <div className="mb-6">
      <h2 className="text-xl font-semibold text-gray-800 mb-2">ML Type</h2>
      <p className="text-gray-600">{scenario.mlType}</p>
    </div>

    <div className="mb-6">
      <h2 className="text-xl font-semibold text-gray-800 mb-2">Description</h2>
      <p className="text-gray-600 whitespace-pre-wrap">{scenario.description}</p>
    </div>
  </div>
);

export const ScenarioDetailPage = () => {
  const { scenarioId } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const { showNotification } = useNotification();
  const [scenario, setScenario] = useState<Scenario | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchScenario = async () => {
      try {
        setLoading(true);
        const data = await scenarioApi.get(scenarioId!);
        setScenario(data);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'An error occurred';
        setError(errorMessage);
        showNotification('danger', errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchScenario();
  }, [scenarioId, scenarioApi, showNotification]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-[800px] mx-auto px-8 py-12">
        <SlAlert variant="danger" className="text-center">
          {error}
        </SlAlert>
      </div>
    );
  }

  if (!scenario) {
    return (
      <div className="max-w-[800px] mx-auto px-8 py-12">
        <SlAlert variant="warning" className="text-center">
          Scenario not found
        </SlAlert>
      </div>
    );
  }

  const currentPath = location.pathname;
  const baseUrl = `/scenarios/${scenarioId}`;

  return (
    <div className="max-w-[1200px] mx-auto px-8 py-12">
      <div className="mb-8">
        <button 
          onClick={() => navigate('/scenarios')}
          className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
        >
          <SlIcon name="arrow-left" className="mr-2" />
          Back to Scenarios
        </button>
        
        <div className="bg-white rounded-lg shadow">
          {/* Header */}
          <div className="p-6 border-b border-gray-200">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h1 className="text-3xl font-bold text-gray-900 mb-2">{scenario.name}</h1>
                <div className="text-sm text-gray-500 mb-4">
                  <span className="inline-flex items-center">
                    <SlIcon name="calendar" className="mr-2" />
                    Created on {new Date(scenario.createdAt).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric'
                    })}
                  </span>
                </div>
                {/* ML Type Badge */}
                <div className="mb-3">
                  <span className="px-3 py-1 text-sm font-medium rounded-md bg-indigo-50 text-indigo-700 border border-indigo-100">
                    {scenario.mlType}
                  </span>
                </div>
                <div className="flex flex-wrap gap-2">
                  {scenario.tags.map(tag => (
                    <SlTag key={tag} variant="neutral">{tag}</SlTag>
                  ))}
                </div>
              </div>
              <SlButton variant="primary" onClick={() => navigate(`${baseUrl}/edit`)}>
                Edit Scenario
              </SlButton>
            </div>

            {/* Tab Navigation */}
            <div className="flex gap-2 mt-6 bg-gray-100 p-1 rounded-md">
              <TabItem to={baseUrl} current={currentPath === baseUrl}>
                Overview
              </TabItem>
              <TabItem to={`${baseUrl}/data`} current={currentPath.includes('/data')}>
                Data
              </TabItem>
              <TabItem to={`${baseUrl}/jobs`} current={currentPath.includes('/jobs')}>
                Jobs
              </TabItem>
              <TabItem to={`${baseUrl}/models`} current={currentPath.includes('/models')}>
                Models
              </TabItem>
              <TabItem to={`${baseUrl}/workflows`} current={currentPath.includes('/workflows')}>
                Workflows
              </TabItem>
            </div>
          </div>

          {/* Tab Content */}
          <Routes>
            <Route index element={<ScenarioOverview scenario={scenario} />} />
            <Route path="data" element={<DataPage />} />
            <Route path="jobs" element={<JobListPage />} />
            <Route path="models" element={<MLModelListPage />} />
            <Route path="workflows" element={<ScenarioWorkflowsPage />} />
          </Routes>
        </div>
      </div>
    </div>
  );
};
