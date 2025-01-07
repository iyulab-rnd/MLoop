import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";

interface DataHeaderProps {
  onUpload: () => void;
  uploading: boolean;
}

export const DataHeader: React.FC<DataHeaderProps> = ({ onUpload, uploading }) => {
  return (
    <div className="flex justify-between items-center mb-4">
      <div>
        <h2 className="text-2xl font-semibold">Dataset List</h2>
        <p className="text-gray-600 mt-1">
          View and manage datasets for this scenario
        </p>
      </div>
      <div>
        <SlButton
          variant="primary"
          onClick={onUpload}
          loading={uploading}
        >
          <SlIcon slot="prefix" name="upload" />
          Upload Files
        </SlButton>
      </div>
    </div>
  );
};