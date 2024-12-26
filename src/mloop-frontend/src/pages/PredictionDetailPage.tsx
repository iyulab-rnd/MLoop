import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";
import { predictionsApi } from "../api/predictions";
import { useNotification } from "../contexts/NotificationContext";
import { PredictionFile } from "../types/Prediction";
import FileViewer from "../components/FileViewer";

export const PredictionDetailPage = () => {
  const { showNotification } = useNotification();
  const { scenarioId, predictionId } = useParams();
  const navigate = useNavigate();
  const [files, setFiles] = useState<PredictionFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedFile, setSelectedFile] = useState<{
    url: string;
    name: string;
  } | null>(null);

  useEffect(() => {
    fetchFiles();
  }, [scenarioId, predictionId]);

  const fetchFiles = async () => {
    if (!scenarioId || !predictionId) return;

    try {
      setLoading(true);
      const data = await predictionsApi.listFiles(scenarioId, predictionId);
      setFiles(data);
    } catch (error) {
      console.error('Error fetching files:', error);
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to load files"
      );
    } finally {
      setLoading(false);
    }
  };

  const formatFileSize = (bytes: number) => {
    const units = ['B', 'KB', 'MB', 'GB'];
    let size = bytes;
    let unitIndex = 0;
    
    while (size >= 1024 && unitIndex < units.length - 1) {
      size /= 1024;
      unitIndex++;
    }
    
    return `${size.toFixed(1)} ${units[unitIndex]}`;
  };

  const isPreviewable = (filename: string) => {
    const ext = filename.split('.').pop()?.toLowerCase();
    return ext === 'csv' || ext === 'tsv';
  };

  const handleFileAction = (file: PredictionFile) => {
    if (!scenarioId || !predictionId) return;
    
    const fileUrl = `/api/scenarios/${scenarioId}/predictions/${predictionId}/files/${file.path}`;
    
    if (isPreviewable(file.name)) {
      setSelectedFile({ url: fileUrl, name: file.name });
    } else {
      const a = document.createElement('a');
      a.href = fileUrl;
      a.download = file.name;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
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
      <div className="mb-6">
        <button
          onClick={() => navigate(`/scenarios/${scenarioId}/predictions`)}
          className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
        >
          <SlIcon name="arrow-left" className="mr-2" />
          Back to Predictions
        </button>

        <div className="flex justify-between items-center">
          <div>
            <h2 className="text-2xl font-semibold">Files</h2>
            <p className="text-gray-600 mt-1">
              View and download prediction files
            </p>
          </div>
          <div className="flex gap-2">
            <SlButton
              variant="primary"
              onClick={() => fetchFiles()}
            >
              <SlIcon slot="prefix" name="arrow-clockwise" />
              Refresh
            </SlButton>
          </div>
        </div>
      </div>

      {files.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
          <p>No files available for this prediction.</p>
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
                  File Name
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Size
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Last Modified
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
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
                      onClick={() => handleFileAction(file)}
                    >
                      <SlIcon slot="prefix" name={isPreviewable(file.name) ? "eye" : "download"} />
                      {isPreviewable(file.name) ? 'View' : 'Download'}
                    </SlButton>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {selectedFile && (
        <FileViewer
          url={selectedFile.url}
          fileName={selectedFile.name}
          open={true}
          onClose={() => setSelectedFile(null)}
        />
      )}
    </div>
  );
}
