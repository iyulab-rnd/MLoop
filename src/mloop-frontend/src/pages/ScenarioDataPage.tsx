import React, { useState, useEffect } from 'react';
import { useOutletContext } from 'react-router-dom';
import { SlButton, SlIcon, SlAlert } from '@shoelace-style/shoelace/dist/react';
import { Scenario } from '../types/scenarios';

interface DataFile {
  name: string;
  path: string;
  size: number;
  lastModified: string;
}

type ScenarioContextType = {
  scenario: Scenario;
};

export const ScenarioDataPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  const [files, setFiles] = useState<DataFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    fetchFiles();
  }, [scenario.scenarioId]);

  const fetchFiles = async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/scenarios/${scenario.scenarioId}/data`);
      if (!response.ok) {
        throw new Error('Failed to fetch files');
      }
      const data = await response.json();
      setFiles(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load files');
    } finally {
      setLoading(false);
    }
  };

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setUploading(true);
    setError(null);
    
    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await fetch(`/api/scenarios/${scenario.scenarioId}/data`, {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        throw new Error('Failed to upload file');
      }

      await fetchFiles();
      setSuccessMessage('File uploaded successfully');
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to upload file');
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async (fileName: string) => {
    try {
      const response = await fetch(`/api/scenarios/${scenario.scenarioId}/data/${fileName}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        throw new Error('Failed to delete file');
      }

      await fetchFiles();
      setSuccessMessage('File deleted successfully');
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete file');
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

  return (
    <div className="p-6">
      <div className="mb-6 flex justify-between items-center">
        <h2 className="text-2xl font-semibold">Dataset List</h2>
        <div>
          <input
            type="file"
            id="fileUpload"
            className="hidden"
            onChange={handleFileUpload}
            disabled={uploading}
          />
          <SlButton
            variant="primary"
            onClick={() => document.getElementById('fileUpload')?.click()}
            loading={uploading}
          >
            <SlIcon slot="prefix" name="upload" />
            Upload Dataset
          </SlButton>
        </div>
      </div>

      {error && (
        <SlAlert variant="danger" className="mb-4">
          {error}
        </SlAlert>
      )}

      {successMessage && (
        <SlAlert variant="success" className="mb-4">
          {successMessage}
        </SlAlert>
      )}

      {loading ? (
        <div className="flex items-center justify-center h-48">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        </div>
      ) : files.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
          <p>No datasets available for {scenario.name} yet.</p>
          <p className="mt-2">Click the button above to upload your first dataset.</p>
        </div>
      ) : (
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
                  <td className="px-6 py-4 whitespace-nowrap">
                    {new Date(file.lastModified).toLocaleString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right">
                    <SlButton
                      variant="danger"
                      size="small"
                      onClick={() => handleDelete(file.name)}
                    >
                      <SlIcon slot="prefix" name="trash" />
                      Delete
                    </SlButton>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default ScenarioDataPage;