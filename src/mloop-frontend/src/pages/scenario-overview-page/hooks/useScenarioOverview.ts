import { useState, useEffect } from "react";
import { useNotification } from "../../../hooks/useNotification";
import { scenarioApi } from "../../../api/scenarios";
import { Model } from "../../../types/Model";

export const useScenarioOverview = (scenarioId: string) => {
  const { showNotification } = useNotification();
  const [models, setModels] = useState<Model[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchModels = async () => {
      try {
        setLoading(true);
        const data = await scenarioApi.listModels(scenarioId);
        setModels(data);
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : "Failed to fetch models";
        setError(errorMessage);
        showNotification("danger", errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchModels();
  }, [scenarioId, showNotification]);

  const getBestScore = () => {
    if (models.length === 0) return null;
    return Math.max(...models.map((model) => model.metrics.BestScore));
  };

  const getLatestTraining = () => {
    if (models.length === 0) return null;
    return new Date(
      Math.max(...models.map((model) => new Date(model.createdAt).getTime()))
    );
  };

  return {
    models,
    loading,
    error,
    getBestScore,
    getLatestTraining,
  };
};
