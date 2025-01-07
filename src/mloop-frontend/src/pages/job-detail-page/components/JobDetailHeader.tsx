
import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";
import { Job } from "../../../types/Job";

interface JobDetailHeaderProps {
  job: Job;
  onCancel: () => void;
  scenarioId: string;
  onNavigateBack: () => void;
}

export const JobDetailHeader: React.FC<JobDetailHeaderProps> = ({
  job,
  onCancel,
  onNavigateBack,
}) => {
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

  return (
    <div className="mb-6">
      <button
        onClick={onNavigateBack}
        className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
      >
        <SlIcon name="arrow-left" className="mr-2" />
        Back to Jobs
      </button>

      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">Job Details</h1>
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
          <SlButton variant="danger" onClick={onCancel}>
            <SlIcon slot="prefix" name="x-circle" />
            Cancel Job
          </SlButton>
        )}
      </div>
    </div>
  );
};
