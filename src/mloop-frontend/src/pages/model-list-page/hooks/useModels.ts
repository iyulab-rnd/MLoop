import { useState, useEffect, useCallback } from 'react';
import { Model } from '../../../types/Model';
import { scenarioApi } from '../../../api/scenarios';
import { useNotification } from '../../../hooks/useNotification';
import { useNavigate } from 'react-router-dom';
import { ApiError } from '../../../api/client';

export const useModels = (scenarioId: string) => {
  const { showNotification } = useNotification();
  const navigate = useNavigate();
  const [models, setModels] = useState<Model[]>([]);
  const [loading, setLoading] = useState(true);
  const [cleanupInProgress, setCleanupInProgress] = useState(false);
  const [trainingInProgress, setTrainingInProgress] = useState(false);

  const fetchModels = useCallback(async () => {
    try {
      setLoading(true);
      const data = await scenarioApi.listModels(scenarioId);
      setModels(data);
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to load models"
      );
    } finally {
      setLoading(false);
    }
  }, [scenarioId, showNotification]);

  useEffect(() => {
    fetchModels();
  }, [fetchModels]);

  const handleStartTraining = async () => {
    try {
      setTrainingInProgress(true);
      const result = await scenarioApi.startTraining(scenarioId);
      showNotification("success", "Training started successfully");
      navigate(`/scenarios/${scenarioId}/jobs/${result.jobId}`);
    } catch (error) {
      console.error("Start Training Error:", error);
      let errorMessage = "Failed to start training";
      if (error instanceof Error) {
        errorMessage = error.message;
      } else if (error instanceof ApiError) {
        errorMessage = error.message || errorMessage;
      }
      showNotification("danger", errorMessage);
    } finally {
      setTrainingInProgress(false);
    }
  };

  const handleCleanup = async () => {
    try {
      setCleanupInProgress(true);
      const result = await scenarioApi.cleanupModels(scenarioId);
      showNotification(
        "success",
        result.message || "Models cleaned up successfully"
      );
      await fetchModels();
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to cleanup models"
      );
    } finally {
      setCleanupInProgress(false);
    }
  };

  const handleDeleteModel = async (modelId: string) => {
    try {
      await scenarioApi.deleteModel(scenarioId, modelId);
      showNotification("success", "Model deleted successfully");
      await fetchModels();
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to delete model"
      );
    }
  };

  return {
    models,
    loading,
    cleanupInProgress,
    trainingInProgress,
    handleStartTraining,
    handleCleanup,
    handleDeleteModel,
  };
};
