
import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";

interface JobLogsProps {
  logs: string;
  elapsedTime: string;
  isRunning: boolean;
  onRefresh: () => void;
  onScroll: () => void;
  logsRef: React.RefObject<HTMLDivElement>;
  autoScroll: boolean;
}

export const JobLogs: React.FC<JobLogsProps> = ({
  logs,
  elapsedTime,
  isRunning,
  onRefresh,
  onScroll,
  logsRef,
}) => {

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <div className="flex justify-end mb-4">
        <SlButton size="small" variant="default" onClick={onRefresh}>
          <SlIcon slot="prefix" name="arrow-clockwise" />
          Refresh Logs
        </SlButton>
      </div>
      <div
        ref={logsRef}
        onScroll={onScroll}
        className="font-mono text-sm whitespace-pre-wrap bg-gray-50 p-4 rounded-lg max-h-[600px] overflow-auto"
      >
        {logs || "No logs available"}
      </div>
      {isRunning && (
        <div className="mt-2 text-sm text-gray-600 flex items-center justify-between">
          <div className="flex items-center">
            <div className="animate-pulse mr-2 h-2 w-2 rounded-full bg-blue-500"></div>
            <span>Running</span>
          </div>
          <div>Elapsed Time: {elapsedTime}</div>
        </div>
      )}
    </div>
  );
};
