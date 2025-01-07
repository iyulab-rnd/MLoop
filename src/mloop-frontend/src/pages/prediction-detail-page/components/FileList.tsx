import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";
import { PredictionFile } from "../../../types/Prediction";
import { formatFileSize } from "../../../utils/formatters";

interface FileListProps {
  files: PredictionFile[];
  onFileAction: (file: PredictionFile) => void;
}

export const FileList: React.FC<FileListProps> = ({ files, onFileAction }) => {
  const isPreviewable = (filename: string) => {
    const ext = filename.split(".").pop()?.toLowerCase();
    return ext === "csv" || ext === "tsv";
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              File Name
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Size
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Last Modified
            </th>
            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {files.map((file) => (
            <tr key={file.path}>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="flex items-center">
                  <SlIcon name="file-earmark" className="mr-2" />
                  {file.name}
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                {formatFileSize(file.size)}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {new Date(file.lastModified).toLocaleString()}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-right">
                <SlButton
                  variant="primary"
                  size="small"
                  onClick={() => onFileAction(file)}
                >
                  <SlIcon
                    slot="prefix"
                    name={isPreviewable(file.name) ? "eye" : "download"}
                  />
                  {isPreviewable(file.name) ? "View" : "Download"}
                </SlButton>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
