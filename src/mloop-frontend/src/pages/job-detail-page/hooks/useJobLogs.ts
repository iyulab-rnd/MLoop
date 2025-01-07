
import { useState, useEffect, useRef } from "react";
import { scenarioApi } from "../../../api/scenarios";
import { useNotification } from "../../../hooks/useNotification";
import { Job } from "../../../types/Job";


export const useJobLogs = (
  scenarioId: string | undefined,
  jobId: string | undefined,
  job: Job | null,
  activeTab: string
) => {
  const [logs, setLogs] = useState<string>("");
  const [elapsedTime, setElapsedTime] = useState<string>("00:00:00");
  const autoScrollRef = useRef<HTMLDivElement>(null);
  const [autoScroll, setAutoScroll] = useState(true);
  const { showNotification } = useNotification();

  const fetchLogs = async () => {
    if (!scenarioId || !jobId || !job || activeTab !== "logs") return;

    try {
      const data = await scenarioApi.getJobLogs(scenarioId, jobId);
      setLogs(data);
    } catch (error) {
      console.error("Error fetching logs:", error);
      setLogs("Failed to load logs");
      showNotification("warning", "Failed to load job logs");
    }
  };

  useEffect(() => {
    let intervalId: number | undefined;

    if (job?.status.toLowerCase() === "running" && activeTab === "logs") {
      fetchLogs();
      intervalId = window.setInterval(fetchLogs, 3000);
    } else {
      fetchLogs();
    }

    return () => {
      if (intervalId) {
        clearInterval(intervalId);
      }
    };
  }, [activeTab, job, scenarioId, jobId]);

  useEffect(() => {
    if (!job || job.status.toLowerCase() !== "running") return;

    const startTime = new Date(job.startedAt).getTime();
    const updateElapsedTime = () => {
      const now = new Date().getTime();
      const elapsed = now - startTime;

      const hours = Math.floor(elapsed / (1000 * 60 * 60));
      const minutes = Math.floor((elapsed % (1000 * 60 * 60)) / (1000 * 60));
      const seconds = Math.floor((elapsed % (1000 * 60)) / 1000);

      setElapsedTime(
        `${hours.toString().padStart(2, "0")}:${minutes
          .toString()
          .padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`
      );
    };

    updateElapsedTime();
    const intervalId = window.setInterval(updateElapsedTime, 1000);

    return () => clearInterval(intervalId);
  }, [job]);

  const handleRefreshLogs = async () => {
    if (!scenarioId || !jobId || !job || activeTab !== "logs") return;

    try {
      await fetchLogs();
      showNotification("success", "Logs refreshed successfully");
    } catch (err) {
      console.error("Error refreshing logs:", err);
      showNotification("danger", "Failed to refresh logs");
    }
  };

  const handleScroll = () => {
    if (!autoScrollRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = autoScrollRef.current;
    const isAtBottom = Math.abs(scrollHeight - clientHeight - scrollTop) < 50;
    setAutoScroll(isAtBottom);
  };

  return {
    logs,
    elapsedTime,
    handleRefreshLogs,
    handleScroll,
    autoScrollRef,
    autoScroll,
  };
};