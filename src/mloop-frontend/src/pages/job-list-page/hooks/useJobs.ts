import { useState, useCallback } from "react";
import { Job } from "../../../types/Job";
import { scenarioApi } from "../../../api/scenarios";
import { useNotification } from "../../../hooks/useNotification";

export const useJobs = (scenarioId: string) => {
  const { showNotification } = useNotification();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [cleanupInProgress, setCleanupInProgress] = useState(false);

  const fetchJobs = useCallback(async () => {
    try {
      setLoading(true);
      const data: Job[] = await scenarioApi.listJobs(scenarioId);
      const sortedJobs = data.sort(
        (a: Job, b: Job) =>
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      );
      setJobs(sortedJobs);
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to load jobs"
      );
    } finally {
      setLoading(false);
    }
  }, [scenarioId, showNotification]);

  const handleCleanup = async () => {
    try {
      setCleanupInProgress(true);
      const result: { message?: string } = await scenarioApi.cleanupJobs(
        scenarioId
      );
      showNotification(
        "success",
        result.message || "Jobs cleaned up successfully"
      );
      await fetchJobs();
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to cleanup jobs"
      );
    } finally {
      setCleanupInProgress(false);
    }
  };

  return {
    jobs,
    loading,
    cleanupInProgress,
    fetchJobs,
    handleCleanup,
  };
};