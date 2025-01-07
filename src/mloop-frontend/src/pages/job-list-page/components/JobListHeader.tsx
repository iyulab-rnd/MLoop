import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";

interface JobListHeaderProps {
  scenarioName: string;
  jobCount: number;
  onCleanup: () => void;
  cleanupInProgress: boolean;
}

export const JobListHeader: React.FC<JobListHeaderProps> = ({
  jobCount,
  onCleanup,
  cleanupInProgress,
}) => {
  return (
    <div className="flex justify-between items-center mb-6">
      <div>
        <h2 className="text-2xl font-semibold">Training Jobs</h2>
        <p className="text-gray-600 mt-1">
          View and manage training jobs for this scenario
        </p>
      </div>
      {jobCount > 0 && (
        <SlButton
          variant="neutral"
          onClick={onCleanup}
          loading={cleanupInProgress}
        >
          <SlIcon slot="prefix" name="trash" />
          Cleanup
        </SlButton>
      )}
    </div>
  );
};