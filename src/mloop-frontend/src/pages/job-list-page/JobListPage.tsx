import { useEffect } from "react";
import { useOutletContext } from "react-router-dom";
import { Scenario } from "../../types/Scenario";
import { LoadingSpinner } from "../../components/common/LoadingSpinner";
import { JobListHeader } from "./components/JobListHeader";
import { EmptyJobState } from "./components/EmptyJobState";
import { JobTable } from "./components/JobTable";
import { useJobs } from "./hooks/useJobs";

type ScenarioContextType = {
  scenario: Scenario;
};

export const JobListPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  const { jobs, loading, cleanupInProgress, fetchJobs, handleCleanup } = useJobs(
    scenario.scenarioId
  );

  useEffect(() => {
    fetchJobs();
  }, [fetchJobs]);

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <div className="p-6">
      <JobListHeader
        scenarioName={scenario.name}
        jobCount={jobs.length}
        onCleanup={handleCleanup}
        cleanupInProgress={cleanupInProgress}
      />

      {jobs.length === 0 ? (
        <EmptyJobState scenarioName={scenario.name} />
      ) : (
        <JobTable jobs={jobs} scenarioId={scenario.scenarioId} />
      )}
    </div>
  );
};