import { useState, useEffect, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  SlIcon,
  SlTabGroup,
  SlTab,
  SlTabPanel,
  SlAlert,
  SlButton,
} from "@shoelace-style/shoelace/dist/react";
import { Job } from "../types/Job";
import { scenarioApi } from "../api/scenarios";
import { useNotification } from "../hooks/useNotification";

export const JobDetailPage = () => {
  const { scenarioId, jobId } = useParams();
  const navigate = useNavigate();
  const { showNotification } = useNotification();
  const [job, setJob] = useState<Job | null>(null);
  const [logs, setLogs] = useState<string>("");
  const [activeTab, setActiveTab] = useState("details");
  const [elapsedTime, setElapsedTime] = useState<string>("00:00:00");
  const logsRef = useRef<HTMLDivElement>(null);
  const autoScrollRef = useRef(true);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchJobDetails = async () => {
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
    };

    fetchJobDetails();
  }, [scenarioId, jobId, showNotification]); // Removed scenarioApi

  useEffect(() => {
    const fetchLogs = async () => {
      if (!scenarioId || !jobId || !job || activeTab !== "logs") return;

      try {
        const data = await scenarioApi.getJobLogs(scenarioId, jobId);
        setLogs(data);
        
        // Auto scroll to bottom if enabled
        if (autoScrollRef.current && logsRef.current) {
          logsRef.current.scrollTop = logsRef.current.scrollHeight;
        }
      } catch (error) {
        console.error('Error fetching logs:', error);
        setLogs('Failed to load logs');
        showNotification('warning', 'Failed to load job logs');
      }
    };

    // Set up auto-refresh for running jobs
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
  }, [activeTab, job, scenarioId, jobId, showNotification]);

  // Calculate and update elapsed time
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
        `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
      );
    };

    updateElapsedTime();
    const intervalId = window.setInterval(updateElapsedTime, 1000);

    return () => clearInterval(intervalId);
  }, [job]);

  const handleCancelJob = async () => {
    if (!scenarioId || !jobId || !job || job.status.toLowerCase() !== "running")
      return;

    try {
      await scenarioApi.cancelJob(scenarioId, jobId);
      const updatedJob = await scenarioApi.getJob(scenarioId, jobId);
      setJob(updatedJob);
      showNotification("success", "Job cancelled successfully");
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to cancel job";
      showNotification("danger", errorMessage);
    }
  };

  const handleRefreshLogs = async () => {
    if (!scenarioId || !jobId || !job || activeTab !== "logs") return;

    try {
      const data = await scenarioApi.getJobLogs(scenarioId, jobId);
      setLogs(data);
      if (autoScrollRef.current && logsRef.current) {
        logsRef.current.scrollTop = logsRef.current.scrollHeight;
      }
      showNotification("success", "Logs refreshed successfully");
    } catch (err) {
      console.error("Error refreshing logs:", err);
      showNotification("danger", "Failed to refresh logs");
    }
  };

  const handleScroll = () => {
    if (!logsRef.current) return;
    
    const { scrollTop, scrollHeight, clientHeight } = logsRef.current;
    const isAtBottom = Math.abs(scrollHeight - clientHeight - scrollTop) < 50;
    autoScrollRef.current = isAtBottom;
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case "running":
        return "bg-blue-100 text-blue-800";
      case "completed":
        return "bg-green-100 text-green-800";
      case "failed":
        return "bg-red-100 text-red-800";
      case "cancelled":
        return "bg-gray-100 text-gray-800";
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

  if (error || !job) {
    return (
      <div className="p-6">
        <SlAlert variant="danger">{error || "Job not found"}</SlAlert>
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="mb-6">
        <button
          onClick={() => navigate(`/scenarios/${scenarioId}/jobs`)}
          className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
        >
          <SlIcon name="arrow-left" className="mr-2" />
          Back to Jobs
        </button>

        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">
              Job Details
            </h1>
            <div className="flex items-center gap-4">
              <span className="px-3 py-1 text-sm font-medium rounded-md bg-indigo-50 text-indigo-700 border border-indigo-100">
                {job.jobType}
              </span>
              <span
                className={`px-3 py-1 text-sm font-medium rounded-md ${getStatusColor(
                  job.status
                )}`}
              >
                {job.status}
              </span>
            </div>
          </div>
          {job.status.toLowerCase() === "running" && (
            <SlButton variant="danger" onClick={handleCancelJob}>
              <SlIcon slot="prefix" name="x-circle" />
              Cancel Job
            </SlButton>
          )}
        </div>
      </div>

      <SlTabGroup>
        <SlTab
          slot="nav"
          panel="details"
          active={activeTab === "details"}
          onClick={() => setActiveTab("details")}
        >
          Details
        </SlTab>
        <SlTab
          slot="nav"
          panel="logs"
          active={activeTab === "logs"}
          onClick={() => setActiveTab("logs")}
        >
          Logs
        </SlTab>

        <SlTabPanel name="details">
          <div className="space-y-8">
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-medium mb-4">Job Information</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <h4 className="text-sm font-medium text-gray-500 mb-1">
                    Job ID
                  </h4>
                  <p className="text-sm text-gray-900">{job.jobId}</p>
                </div>
                {job.modelId && (
                  <div>
                    <h4 className="text-sm font-medium text-gray-500 mb-1">
                      Model ID
                    </h4>
                    <p className="text-sm text-gray-900">{job.modelId}</p>
                  </div>
                )}
                <div>
                  <h4 className="text-sm font-medium text-gray-500 mb-1">
                    Worker ID
                  </h4>
                  <p className="text-sm text-gray-900">{job.workerId}</p>
                </div>
                <div>
                  <h4 className="text-sm font-medium text-gray-500 mb-1">
                    Created At
                  </h4>
                  <p className="text-sm text-gray-900">
                    {new Date(job.createdAt).toLocaleString()}
                  </p>
                </div>
                {job.startedAt && (
                  <div>
                    <h4 className="text-sm font-medium text-gray-500 mb-1">
                      Started At
                    </h4>
                    <p className="text-sm text-gray-900">
                      {new Date(job.startedAt).toLocaleString()}
                    </p>
                  </div>
                )}
                {job.failureType !== "None" && (
                  <div>
                    <h4 className="text-sm font-medium text-gray-500 mb-1">
                      Failure Type
                    </h4>
                    <p className="text-sm text-red-600">{job.failureType}</p>
                  </div>
                )}
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-medium mb-4">Status History</h3>
              <div className="space-y-4">
                {job.statusHistory.map((history, index) => (
                  <div
                    key={index}
                    className="flex items-start gap-4 p-4 bg-gray-50 rounded-lg"
                  >
                    <div
                      className={`w-2 h-2 rounded-full mt-2 ${getStatusColor(
                        history.status
                      )}`}
                    />
                    <div className="flex-1">
                      <div className="flex justify-between items-start">
                        <span
                          className={`px-2 py-1 text-xs font-medium rounded-md ${getStatusColor(
                            history.status
                          )}`}
                        >
                          {history.status}
                        </span>
                        <span className="text-sm text-gray-500">
                          {new Date(history.timestamp).toLocaleString()}
                        </span>
                      </div>
                      <p className="mt-2 text-sm text-gray-600">
                        {history.message}
                      </p>
                      {history.workerId && (
                        <p className="mt-1 text-sm text-gray-500">
                          Worker: {history.workerId}
                        </p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </SlTabPanel>

        <SlTabPanel name="logs">
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex justify-end mb-4">
              <SlButton
                size="small"
                variant="default"
                onClick={handleRefreshLogs}
              >
                <SlIcon slot="prefix" name="arrow-clockwise" />
                Refresh Logs
              </SlButton>
            </div>
            <div
              ref={logsRef}
              onScroll={handleScroll}
              className="font-mono text-sm whitespace-pre-wrap bg-gray-50 p-4 rounded-lg max-h-[600px] overflow-auto"
            >
              {logs || "No logs available"}
            </div>
            {job.status.toLowerCase() === "running" && (
              <div className="mt-2 text-sm text-gray-600 flex items-center justify-between">
                <div className="flex items-center">
                  <div className="animate-pulse mr-2 h-2 w-2 rounded-full bg-blue-500"></div>
                  <span>Running</span>
                </div>
                <div>Elapsed Time: {elapsedTime}</div>
              </div>
            )}
          </div>
        </SlTabPanel>
      </SlTabGroup>
    </div>
  );
};