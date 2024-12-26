import { useState, useEffect, useCallback } from "react";
import { useOutletContext, useNavigate } from "react-router-dom";
import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";
import { Scenario } from "../types/Scenario";
import { Job } from "../types/Job";
import { scenarioApi } from "../api/scenarios";
import { useNotification } from "../hooks/useNotification";

type ScenarioContextType = {
  scenario: Scenario;
};

export const JobListPage = () => {
  const { showNotification } = useNotification();
  const { scenario } = useOutletContext<ScenarioContextType>();
  const navigate = useNavigate();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [cleanupInProgress, setCleanupInProgress] = useState(false);

  const fetchJobs = useCallback(async () => {
    try {
      setLoading(true);
      const data: Job[] = await scenarioApi.listJobs(scenario.scenarioId);
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
  }, [scenario.scenarioId, showNotification]);

  useEffect(() => {
    fetchJobs();
  }, [fetchJobs]);

  const handleCleanup = async () => {
    try {
      setCleanupInProgress(true);
      const result: { message?: string } = await scenarioApi.cleanupJobs(
        scenario.scenarioId
      ); // Define the expected type
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

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-2xl font-semibold">Training Jobs</h2>
          <p className="text-gray-600 mt-1">
            View and manage training jobs for this scenario
          </p>
        </div>
        {jobs.length > 0 && (
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

      {jobs.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
          <p>No jobs have been run for {scenario.name} yet.</p>
          <p className="mt-2">Start a new training job to see results here.</p>
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
                  Type
                </th>
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
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {jobs.map((job) => (
                <tr
                  key={job.jobId}
                  className="hover:bg-gray-50 cursor-pointer"
                  onClick={() =>
                    navigate(`/scenarios/${scenario.scenarioId}/jobs/${job.jobId}`)
                  }
                >
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="px-2 py-1 text-xs font-medium rounded-md bg-indigo-50 text-indigo-700">
                      {job.jobType}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span
                      className={`px-2 py-1 text-xs font-medium rounded-md ${getStatusColor(
                        job.status
                      )}`}
                    >
                      {job.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {new Date(job.createdAt).toLocaleString()}
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
