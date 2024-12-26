import { useState, useEffect, useCallback } from "react";
import { useOutletContext, useNavigate } from "react-router-dom";
import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";
import { Scenario } from "../types/Scenario";
import { Prediction } from "../types/Prediction";
import { predictionsApi } from "../api/predictions";
import { useNotification } from "../hooks/useNotification";

type ScenarioContextType = {
  scenario: Scenario;
};

export const PredictionsPage = () => {
  const { showNotification } = useNotification();
  const { scenario } = useOutletContext<ScenarioContextType>();
  const navigate = useNavigate();
  const [predictions, setPredictions] = useState<Prediction[]>([]);
  const [loading, setLoading] = useState(true);
  const [cleanupInProgress, setCleanupInProgress] = useState(false);

  const fetchPredictions = useCallback(async () => {
    try {
      setLoading(true);
      const data = await predictionsApi.list(scenario.scenarioId);
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
  }, [scenario.scenarioId, showNotification]);

  useEffect(() => {
    fetchPredictions();
  }, [fetchPredictions]);
  
  const handleCleanup = async () => {
    try {
      setCleanupInProgress(true);
      const result = await predictionsApi.cleanup(scenario.scenarioId);
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

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case "waiting":
      case "processing":
        return "bg-blue-100 text-blue-800";
      case "completed":
        return "bg-green-100 text-green-800";
      case "failed":
        return "bg-red-100 text-red-800";
      default:
        return "bg-gray-100 text-gray-800";
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
          <h2 className="text-2xl font-semibold">Predictions</h2>
          <p className="text-gray-600 mt-1">
            View and manage predictions for this scenario
          </p>
        </div>
        <div className="flex gap-2">
          {predictions.length > 0 && (
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

      {predictions.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
          <p>No predictions have been made for {scenario.name} yet.</p>
          <p className="mt-2">Start a new prediction to see results here.</p>
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
                  Status
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Created At
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Model ID
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {predictions.map((prediction) => (
                <tr
                  key={prediction.predictionId}
                  className="hover:bg-gray-50 cursor-pointer"
                  onClick={() => navigate(`/scenarios/${scenario.scenarioId}/predictions/${prediction.predictionId}`)}
                >
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span
                      className={`px-2 py-1 text-xs font-medium rounded-md ${getStatusColor(
                        prediction.status
                      )}`}
                    >
                      {prediction.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {new Date(prediction.createdAt).toLocaleString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">
                      {prediction.modelId}
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