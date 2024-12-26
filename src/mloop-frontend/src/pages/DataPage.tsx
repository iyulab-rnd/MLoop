import React, { useState, useEffect } from 'react';
import { useOutletContext } from 'react-router-dom';
import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';
import { Scenario } from '../types/Scenario';
import { DataFile } from '../types/DataFile';
import { scenarioApi } from '../api/scenarios';
import { useNotification } from '../contexts/NotificationContext';

type ScenarioContextType = {
  scenario: Scenario;
};

export const DataPage = () => {
  const { showNotification } = useNotification();
  const { scenario } = useOutletContext<ScenarioContextType>();
  const [files, setFiles] = useState<DataFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    fetchFiles();
  }, [scenario.scenarioId]);

  const fetchFiles = async () => {
    try {
      setLoading(true);
      const data = await scenarioApi.listFiles(scenario.scenarioId);
      setFiles(data);
    } catch (error) {
      showNotification('danger', error instanceof Error ? error.message : 'Failed to fetch files');
    } finally {
      setLoading(false);
    }
  };

  const handleFilesUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = event.target.files;
    if (!selectedFiles || selectedFiles.length === 0) return;
  
    // 파일 크기 제한
    const MAX_SIZE = 200 * 1024 * 1024; // 200MB
    for (let file of selectedFiles) {
      if (file.size > MAX_SIZE) {
        showNotification('danger', `File ${file.name} exceeds the maximum size of 10MB`);
        return;
      }
    }
  
    setUploading(true);
    
    try {
      await scenarioApi.uploadFiles(scenario.scenarioId, Array.from(selectedFiles));
      await fetchFiles();
      showNotification('success', `${selectedFiles.length} file(s) uploaded successfully`);
    } catch (error) {
      showNotification('danger', error instanceof Error ? error.message : 'Failed to upload files');
    } finally {
      setUploading(false);
      // 동일한 파일을 다시 업로드할 수 있도록 입력 값을 초기화합니다.
      event.target.value = '';
    }
  };
  

  const handleDelete = async (fileName: string) => {
    try {
      await scenarioApi.deleteFile(scenario.scenarioId, fileName);
      await fetchFiles();
      showNotification('success', 'File deleted successfully');
    } catch (error) {
      showNotification('danger', error instanceof Error ? error.message : 'Failed to delete file');
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

  if (loading) {
    return (
      <div className="flex items-center justify-center h-48">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="mb-6 flex justify-between items-center">
        <h2 className="text-2xl font-semibold">Dataset List</h2>
        <div>
          <input
            type="file"
            id="fileUpload"
            className="hidden"
            onChange={handleFilesUpload}
            multiple
            accept=".tsv, .csv, .xlsx" // 필요한 파일 형식을 추가
            disabled={uploading}
          />
          <SlButton
            variant="primary"
            onClick={() => document.getElementById('fileUpload')?.click()}
            loading={uploading}
          >
            <SlIcon slot="prefix" name="upload" />
            Upload Files
          </SlButton>
        </div>
      </div>

      {files.length === 0 ? (
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
