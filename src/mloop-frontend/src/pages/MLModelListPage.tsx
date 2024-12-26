import { useState, useEffect, useCallback } from "react";
import { useOutletContext, useNavigate } from "react-router-dom";
import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";
import { Scenario } from "../types/Scenario";
import { Model } from "../types/Model";
import { scenarioApi } from "../api/scenarios";
import { useNotification } from "../hooks/useNotification";
import { ApiError } from "../api/client";

type ScenarioContextType = {
  scenario: Scenario;
};

export const MLModelListPage = () => {
  const { showNotification } = useNotification();
  const { scenario } = useOutletContext<ScenarioContextType>();
  const navigate = useNavigate();
  const [models, setModels] = useState<Model[]>([]);
  const [loading, setLoading] = useState(true);
  const [cleanupInProgress, setCleanupInProgress] = useState(false);
  const [trainingInProgress, setTrainingInProgress] = useState(false);

  const fetchModels = useCallback(async () => {
    try {
      setLoading(true);
      const data: Model[] = await scenarioApi.listModels(scenario.scenarioId);
      setModels(data);
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to load models"
      );
    } finally {
      setLoading(false);
    }
  }, [scenario.scenarioId, showNotification]);
  
  useEffect(() => {
    fetchModels();
  }, [fetchModels]);

  const handleStartTraining = async () => {
    try {
      setTrainingInProgress(true);
      const result = await scenarioApi.startTraining(scenario.scenarioId);
      showNotification("success", "Training started successfully");
      navigate(`/scenarios/${scenario.scenarioId}/jobs/${result.jobId}`);
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
      const result: { message?: string } = await scenarioApi.cleanupModels(
        scenario.scenarioId
      ); // Define the expected type
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

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-2xl font-semibold">Trained Models</h2>
          <p className="text-gray-600 mt-1">
            View and manage trained models for this scenario
          </p>
        </div>
        <div className="flex gap-2">
          <SlButton
            variant="primary"
            onClick={handleStartTraining}
            loading={trainingInProgress}
          >
            <SlIcon slot="prefix" name="play" />
            Start Training
          </SlButton>
          {models.length > 0 && (
            <SlButton
              variant="neutral"
              onClick={handleCleanup}
              loading={cleanupInProgress}
            >
              <SlIcon slot="prefix" name="trash" />
              Cleanup
            </SlButton>
          )}
        </div>
      </div>

      {models.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
          <p>No models have been trained for {scenario.name} yet.</p>
          <p className="mt-2">Start a training job to create new models.</p>
        </div>
      ) : (
        <div className="bg-white rounded-lg border border-gray-200">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Model ID
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  ML Type
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Best Score
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Runtime
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Created At
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {models.map((model) => (
                <tr key={model.modelId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">
                      {model.modelId}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="px-2 py-1 text-xs font-medium rounded-md bg-indigo-50 text-indigo-700">
                      {model.mlType}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">
                      {(model.metrics.BestScore * 100).toFixed(2)}%
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">
                      {model.metrics.BestRuntime.toFixed(2)}s
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {new Date(model.createdAt).toLocaleString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right">
                    <div className="flex justify-end gap-2">
                      <SlButton
                        size="small"
                        variant="primary"
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(
                            `/scenarios/${scenario.scenarioId}/models/${model.modelId}/predict`
                          );
                        }}
                      >
                        <SlIcon slot="prefix" name="play" />
                        Predict
                      </SlButton>
                      <SlButton
                        size="small"
                        variant="default"
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(
                            `/scenarios/${scenario.scenarioId}/models/${model.modelId}`
                          );
                        }}
                      >
                        <SlIcon slot="prefix" name="eye" />
                        View
                      </SlButton>
                      <SlButton
                        size="small"
                        variant="danger"
                        onClick={async (e) => {
                          e.stopPropagation();
                          try {
                            await scenarioApi.deleteModel(
                              scenario.scenarioId,
                              model.modelId
                            );
                            showNotification(
                              "success",
                              "Model deleted successfully"
                            );
                            await fetchModels();
                          } catch (error) {
                            showNotification(
                              "danger",
                              error instanceof Error
                                ? error.message
                                : "Failed to delete model"
                            );
                          }
                        }}
                      >
                        <SlIcon slot="prefix" name="trash" />
                        Delete
                      </SlButton>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
