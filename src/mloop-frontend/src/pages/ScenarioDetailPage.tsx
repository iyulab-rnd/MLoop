import { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation, Routes, Route, Link } from 'react-router-dom';
import { SlButton, SlIcon, SlTag } from '@shoelace-style/shoelace/dist/react';
import { Scenario } from '../types/scenarios';
import { ScenarioDataPage } from './ScenarioDataPage';
import { ScenarioJobsPage } from './ScenarioJobsPage';
import { ScenarioModelsPage } from './ScenarioModelsPage';
import { ScenarioWorkflowsPage } from './ScenarioWorkflowsPage';

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
  const [scenario, setScenario] = useState<Scenario | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const fetchScenario = async () => {
      try {
        setLoading(true);
        const response = await fetch(`/api/scenarios/${scenarioId}`);
        if (!response.ok) {
          throw new Error('Failed to fetch scenario');
        }
        const data = await response.json();
        setScenario(data);
      } catch (err) {
        setError(err instanceof Error ? err : new Error('An error occurred'));
      } finally {
        setLoading(false);
      }
    };

    fetchScenario();
  }, [scenarioId]);

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
        <div className="p-4 bg-red-50 text-red-600 rounded-lg text-center">
          {error.message}
        </div>
      </div>
    );
  }

  if (!scenario) {
    return (
      <div className="max-w-[800px] mx-auto px-8 py-12">
        <div className="p-4 bg-yellow-50 text-yellow-600 rounded-lg text-center">
          Scenario not found
        </div>
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
            <Route path="data" element={<ScenarioDataPage />} />
            <Route path="jobs" element={<ScenarioJobsPage />} />
            <Route path="models" element={<ScenarioModelsPage />} />
            <Route path="workflows" element={<ScenarioWorkflowsPage />} />
          </Routes>
        </div>
      </div>
    </div>
  );
};