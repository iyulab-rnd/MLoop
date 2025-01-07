import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { scenarioApi } from '../../../api/scenarios';
import { Scenario } from '../../../types/Scenario';
import { Model } from '../../../types/Model';
import { useNotification } from '../../../hooks/useNotification';

export const useScenarioDetail = (scenarioId: string | undefined) => {
  const navigate = useNavigate();
  const { showNotification } = useNotification();
  const [scenario, setScenario] = useState<Scenario | null>(null);
  const [bestModel, setBestModel] = useState<Model | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      if (!scenarioId) return;

      try {
        setLoading(true);
        const scenarioData = await scenarioApi.get(scenarioId);
        setScenario(scenarioData);

        const models = await scenarioApi.listModels(scenarioId);
        const sortedModels = models.sort(
          (a, b) => b.metrics.BestScore - a.metrics.BestScore
        );
        setBestModel(sortedModels[0] || null);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'An error occurred';
        setError(errorMessage);
        showNotification('danger', errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [scenarioId, showNotification]);

  const handleEdit = () => {
    navigate(`/scenarios/${scenarioId}/edit`);
  };

  const handlePredict = (modelId: string) => {
    navigate(`/scenarios/${scenarioId}/models/${modelId}/predict`);
  };

  return {
    scenario,
    bestModel,
    loading,
    error,
    handleEdit,
    handlePredict,
  };
};