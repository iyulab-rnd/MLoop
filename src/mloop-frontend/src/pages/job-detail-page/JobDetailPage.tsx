
import { useParams, useNavigate } from "react-router-dom";
import { SlTabGroup, SlTab, SlTabPanel, SlAlert } from "@shoelace-style/shoelace/dist/react";
import { useState } from "react";
import { JobDetailHeader } from "./components/JobDetailHeader";
import { JobInformation } from "./components/JobInformation";
import { JobLogs } from "./components/JobLogs";
import { useJobDetails } from "./hooks/useJobDetails";
import { useJobLogs } from "./hooks/useJobLogs";

export const JobDetailPage = () => {
  const { scenarioId, jobId } = useParams();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState("details");

  const { job, loading, error, handleCancelJob } = useJobDetails(
    scenarioId,
    jobId
  );

  const { logs, elapsedTime, handleRefreshLogs, handleScroll, autoScrollRef, autoScroll } = useJobLogs(
    scenarioId,
    jobId,
    job,
    activeTab
  );

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
      <JobDetailHeader
        job={job}
        onCancel={handleCancelJob}
        scenarioId={scenarioId!}
        onNavigateBack={() => navigate(`/scenarios/${scenarioId}/jobs`)}
      />

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
          <JobInformation job={job} />
        </SlTabPanel>

        <SlTabPanel name="logs">
          <JobLogs
            logs={logs}
            elapsedTime={elapsedTime}
            isRunning={job.status.toLowerCase() === "running"}
            onRefresh={handleRefreshLogs}
            onScroll={handleScroll}
            logsRef={autoScrollRef}
            autoScroll={autoScroll}
          />
        </SlTabPanel>
      </SlTabGroup>
    </div>
  );
};