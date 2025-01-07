import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  SlIcon,
  SlTabGroup,
  SlTab,
  SlTabPanel,
  SlAlert,
  SlButton,
} from '@shoelace-style/shoelace/dist/react';
import { Model } from '../types/Model';
import { scenarioApi } from '../api/scenarios';
import { useNotification } from "../hooks/useNotification";

import { formatTime } from '../utils/time';

export const MLModelDetailPage = () => {
  const { showNotification } = useNotification();
  const { scenarioId, modelId } = useParams();
  const navigate = useNavigate();
  const [model, setModel] = useState<Model | null>(null);
  const [logs, setLogs] = useState<string>('');
  const [activeTab, setActiveTab] = useState('details');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchModelDetails = async () => {
      if (!scenarioId || !modelId) return;

      try {
        setLoading(true);
        const data: Model = await scenarioApi.getModel(scenarioId, modelId);
        setModel(data);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to load model details';
        setError(errorMessage);
        showNotification('danger', errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchModelDetails();
  }, [scenarioId, modelId, showNotification]);

  useEffect(() => {
    const fetchLogs = async () => {
      if (!scenarioId || !modelId || !model || activeTab !== 'logs') return;

      try {
        const data = await scenarioApi.getModelLogs(scenarioId, modelId);
        setLogs(data);
      } catch (err) {
        console.error(err);
        setLogs('Failed to load logs');
        showNotification('warning', 'Failed to load model logs');
      }
    };

    fetchLogs();
  }, [activeTab, model, scenarioId, modelId, showNotification]);

  const handleRefreshLogs = async () => {
    if (!scenarioId || !modelId || !model || activeTab !== 'logs') return;

    try {
      const data = await scenarioApi.getModelLogs(scenarioId, modelId);
      setLogs(data);
      showNotification('success', 'Logs refreshed successfully');
    } catch (err) {
      console.error(err);
      showNotification('danger', 'Failed to refresh logs');
    }
  };

  const formatMetricValue = (key: string, value: number): string => {
    if (key.toLowerCase().includes('score') || key.toLowerCase().includes('accuracy')) {
      return `${(value * 100).toFixed(2)}%`;
    } 
    if (key.toLowerCase().includes('time') || key.toLowerCase().includes('runtime')) {
      return formatTime(value);
    }
    return Number.isInteger(value) ? value.toString() : value.toFixed(4);
  };

  const formatArgumentValue = (value: string | number | boolean | null): string => {
    if (value === null) return 'null';
    return String(value);
  };
  
  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error || !model) {
    return (
      <div className="p-6">
        <SlAlert variant="danger">
          {error || "Model not found"}
        </SlAlert>
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="mb-6">
        <button 
          onClick={() => navigate(`/scenarios/${scenarioId}/models`)}
          className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
        >
          <SlIcon name="arrow-left" className="mr-2" />
          Back to Models
        </button>

        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Model {model.modelId}</h1>
            <div className="flex items-center gap-4">
              <span className="px-3 py-1 text-sm font-medium rounded-md bg-indigo-50 text-indigo-700 border border-indigo-100">
                {model.mlType}
              </span>
              <span className="text-sm text-gray-500">
                Created on {new Date(model.createdAt).toLocaleString()}
              </span>
            </div>
          </div>
        </div>
      </div>

      <SlTabGroup>
        <SlTab 
          slot="nav" 
          panel="details" 
          active={activeTab === 'details'} 
          onClick={() => setActiveTab('details')}
        >
          Details
        </SlTab>
        <SlTab 
          slot="nav" 
          panel="logs" 
          active={activeTab === 'logs'} 
          onClick={() => setActiveTab('logs')}
        >
          Logs
        </SlTab>

        <SlTabPanel name="details">
          <div className="space-y-8">
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-medium mb-4">Performance Metrics</h3>
              <div className="overflow-hidden">
                <table className="min-w-full">
                  <thead className="bg-gray-50">
                    <tr>
                      <th 
                        scope="col" 
                        className="py-3.5 pl-4 pr-3 text-left text-sm font-semibold text-gray-900 sm:pl-6"
                      >
                        Metric
                      </th>
                      <th 
                        scope="col" 
                        className="px-3 py-3.5 text-left text-sm font-semibold text-gray-900"
                      >
                        Value
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200 bg-white">
                    {Object.entries(model.metrics).sort().map(([key, value]) => (
                      <tr key={key}>
                        <td className="py-4 pl-4 pr-3 text-sm text-gray-600 sm:pl-6">
                          {key.replace(/_/g, ' ')}
                        </td>
                        <td className="px-3 py-4 text-sm font-medium text-gray-900">
                          {formatMetricValue(key, value)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-medium mb-4">Training Arguments</h3>
              <div className="overflow-hidden">
                <table className="min-w-full">
                  <thead className="bg-gray-50">
                    <tr>
                      <th 
                        scope="col" 
                        className="py-3.5 pl-4 pr-3 text-left text-sm font-semibold text-gray-900 sm:pl-6"
                      >
                        Argument
                      </th>
                      <th 
                        scope="col" 
                        className="px-3 py-3.5 text-left text-sm font-semibold text-gray-900"
                      >
                        Value
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200 bg-white">
                    {Object.entries(model.arguments).sort().map(([key, value]) => (
                      <tr key={key}>
                        <td className="py-4 pl-4 pr-3 text-sm text-gray-600 sm:pl-6">
                          {key.replace(/-/g, ' ')}
                        </td>
                        <td className="px-3 py-4 text-sm font-medium text-gray-900 whitespace-pre-wrap break-all">
                          {formatArgumentValue(value)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </SlTabPanel>

        <SlTabPanel name="logs">
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex justify-end mb-4">
              <SlButton
                size="small"
                variant="default"
                onClick={handleRefreshLogs}
              >
                <SlIcon slot="prefix" name="arrow-clockwise" />
                Refresh Logs
              </SlButton>
            </div>
            <div className="font-mono text-sm whitespace-pre-wrap bg-gray-50 p-4 rounded-lg max-h-[600px] overflow-auto">
              {logs || 'No logs available'}
            </div>
          </div>
        </SlTabPanel>
      </SlTabGroup>
    </div>
  );
};