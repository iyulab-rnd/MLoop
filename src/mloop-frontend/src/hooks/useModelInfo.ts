import { useState, useEffect } from 'react';
import { scenarioApi } from '../api/scenarios';
import { Model } from '../types';
import { useNotification } from './useNotification';

export const useModelInfo = (scenarioId: string | undefined, modelId: string | undefined) => {
  const [model, setModel] = useState<Model | null>(null);
  const [loading, setLoading] = useState(true);
  const { showNotification } = useNotification();

  useEffect(() => {
    const fetchModel = async () => {
      if (!scenarioId || !modelId) return;
      
      try {
        setLoading(true);
        const modelData = await scenarioApi.getModel(scenarioId, modelId);
        setModel(modelData);
      } catch (err) {
        showNotification('danger', 'Failed to load model information');
      } finally {
        setLoading(false);
      }
    };
    
    fetchModel();
  }, [scenarioId, modelId, showNotification]);

  return { model, loading };
};