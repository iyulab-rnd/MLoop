import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';

interface ModelLogsProps {
  logs: string;
  onRefresh: () => void;
}

export const ModelLogs: React.FC<ModelLogsProps> = ({ logs, onRefresh }) => {
  return (
    <div className="bg-white rounded-lg shadow p-6">
      <div className="flex justify-end mb-4">
        <SlButton size="small" variant="default" onClick={onRefresh}>
          <SlIcon slot="prefix" name="arrow-clockwise" />
          Refresh Logs
        </SlButton>
      </div>
      <div className="font-mono text-sm whitespace-pre-wrap bg-gray-50 p-4 rounded-lg max-h-[600px] overflow-auto">
        {logs || 'No logs available'}
      </div>
    </div>
  );
};