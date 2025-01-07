import { useState, useEffect, useCallback } from 'react';
import { Model } from '../../../types/Model';
import { scenarioApi } from '../../../api/scenarios';
import { useNotification } from '../../../hooks/useNotification';

export const useModelDetails = (scenarioId: string | undefined, modelId: string | undefined) => {
  const { showNotification } = useNotification();
  const [model, setModel] = useState<Model | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [logs, setLogs] = useState<string>('');
  const [activeTab, setActiveTab] = useState('details');

  const fetchModelDetails = useCallback(async () => {
    if (!scenarioId || !modelId) return;

    try {
      setLoading(true);
      setError(null);
      const data = await scenarioApi.getModel(scenarioId, modelId);
      setModel(data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load model details';
      setError(errorMessage);
      showNotification('danger', errorMessage);
    } finally {
      setLoading(false);
    }
  }, [scenarioId, modelId, showNotification]);

  const fetchLogs = useCallback(async () => {
    if (!scenarioId || !modelId || !model || activeTab !== 'logs') return;

    try {
      const data = await scenarioApi.getModelLogs(scenarioId, modelId);
      setLogs(data);
    } catch (err) {
      console.error(err);
      setLogs('Failed to load logs');
      showNotification('warning', 'Failed to load model logs');
    }
  }, [scenarioId, modelId, model, activeTab, showNotification]);

  useEffect(() => {
    fetchModelDetails();
  }, [fetchModelDetails]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  const handleRefreshLogs = async () => {
    if (!scenarioId || !modelId || !model || activeTab !== 'logs') return;

    try {
      await fetchLogs();
      showNotification('success', 'Logs refreshed successfully');
    } catch (err) {
      console.error(err);
      showNotification('danger', 'Failed to refresh logs');
    }
  };

  return {
    model,
    loading,
    error,
    logs,
    activeTab,
    setActiveTab,
    handleRefreshLogs,
  };
};
