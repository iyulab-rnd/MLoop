
import { Job } from "../../../types/Job";

interface JobInformationProps {
  job: Job;
}

export const JobInformation: React.FC<JobInformationProps> = ({ job }) => {
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
    <div className="space-y-8">
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-medium mb-4">Job Information</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <h4 className="text-sm font-medium text-gray-500 mb-1">Job ID</h4>
            <p className="text-sm text-gray-900">{job.jobId}</p>
          </div>
          {job.modelId && (
            <div>
              <h4 className="text-sm font-medium text-gray-500 mb-1">Model ID</h4>
              <p className="text-sm text-gray-900">{job.modelId}</p>
            </div>
          )}
          <div>
            <h4 className="text-sm font-medium text-gray-500 mb-1">Worker ID</h4>
            <p className="text-sm text-gray-900">{job.workerId}</p>
          </div>
          <div>
            <h4 className="text-sm font-medium text-gray-500 mb-1">Created At</h4>
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
                <p className="mt-2 text-sm text-gray-600">{history.message}</p>
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
  );
};
