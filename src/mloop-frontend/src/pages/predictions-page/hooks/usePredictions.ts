import { useState, useCallback, useEffect } from 'react';
import { useNotification } from '../../../hooks/useNotification';
import { predictionsApi } from '../../../api/predictions';
import { Prediction } from '../../../types/Prediction';

export const usePredictions = (scenarioId: string) => {
  const { showNotification } = useNotification();
  const [predictions, setPredictions] = useState<Prediction[]>([]);
  const [loading, setLoading] = useState(true);
  const [cleanupInProgress, setCleanupInProgress] = useState(false);

  const fetchPredictions = useCallback(async () => {
    try {
      setLoading(true);
      const data = await predictionsApi.list(scenarioId);
      const sortedPredictions = data.sort(
        (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      );
      setPredictions(sortedPredictions);
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to load predictions"
      );
    } finally {
      setLoading(false);
    }
  }, [scenarioId, showNotification]);

  const handleCleanup = async () => {
    try {
      setCleanupInProgress(true);
      const result = await predictionsApi.cleanup(scenarioId);
      showNotification("success", result.message);
      await fetchPredictions();
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to cleanup predictions"
      );
    } finally {
      setCleanupInProgress(false);
    }
  };

  useEffect(() => {
    fetchPredictions();
  }, [fetchPredictions]);

  return {
    predictions,
    loading,
    cleanupInProgress,
    handleCleanup
  };
};
