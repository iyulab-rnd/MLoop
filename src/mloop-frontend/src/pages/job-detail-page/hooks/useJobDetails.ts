
import { useState, useEffect, useCallback } from "react";
import { Job } from "../../../types/Job";
import { scenarioApi } from "../../../api/scenarios";
import { useNotification } from "../../../hooks/useNotification";

export const useJobDetails = (scenarioId: string | undefined, jobId: string | undefined) => {
  const { showNotification } = useNotification();
  const [job, setJob] = useState<Job | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchJobDetails = useCallback(async () => {
    if (!scenarioId || !jobId) return;

    try {
      setLoading(true);
      setError(null);
      const data = await scenarioApi.getJob(scenarioId, jobId);
      setJob(data);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to load job details";
      setError(errorMessage);
      showNotification("danger", errorMessage);
    } finally {
      setLoading(false);
    }
  }, [scenarioId, jobId, showNotification]);

  useEffect(() => {
    fetchJobDetails();
  }, [fetchJobDetails]);

  const handleCancelJob = async () => {
    if (!scenarioId || !jobId || !job || job.status.toLowerCase() !== "running")
      return;

    try {
      await scenarioApi.cancelJob(scenarioId, jobId);
      await fetchJobDetails();
      showNotification("success", "Job cancelled successfully");
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to cancel job";
      showNotification("danger", errorMessage);
    }
  };

  return {
    job,
    loading,
    error,
    handleCancelJob,
    fetchJobDetails,
  };
};
