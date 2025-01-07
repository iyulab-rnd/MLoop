import { useState, useCallback } from 'react';
import { DataFile } from '../../../types/DataFile';
import { scenarioApi } from '../../../api/scenarios';
import { useNotification } from '../../../hooks/useNotification';

export const useDataFiles = (scenarioId: string) => {
  const { showNotification } = useNotification();
  const [files, setFiles] = useState<DataFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);

  const fetchFiles = useCallback(async (currentPath: string = '') => {
    try {
      setLoading(true);
      const data = await scenarioApi.listFiles(
        scenarioId,
        currentPath ? { path: currentPath } : undefined
      );
      setFiles(data);
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to fetch files"
      );
    } finally {
      setLoading(false);
    }
  }, [scenarioId, showNotification]);

  const handleUpload = async (
    files: FileList,
    currentPath: string = ''
  ) => {
    const MAX_SIZE = {
      zip: 500 * 1024 * 1024, // 500MB for zip files
      default: 200 * 1024 * 1024, // 200MB for other files
    };

    for (const file of files) {
      const extension = file.name.split(".").pop()?.toLowerCase();
      const sizeLimit = extension === "zip" ? MAX_SIZE.zip : MAX_SIZE.default;
      const sizeLimitInMB = sizeLimit / (1024 * 1024);

      if (file.size > sizeLimit) {
        showNotification(
          "danger",
          `File ${file.name} exceeds the maximum size of ${sizeLimitInMB}MB`
        );
        return;
      }
    }

    setUploading(true);

    const formData = new FormData();
    Array.from(files).forEach((file) => {
      formData.append("files", file);
    });

    try {
      await fetch(
        `/api/scenarios/${scenarioId}/data${
          currentPath ? `?path=${encodeURIComponent(currentPath)}` : ""
        }`,
        {
          method: "POST",
          body: formData,
        }
      );

      await fetchFiles(currentPath);
      showNotification(
        "success",
        `${files.length} file(s) uploaded successfully`
      );
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to upload files"
      );
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async (filePath: string, currentPath: string = '') => {
    try {
      await scenarioApi.deleteFile(scenarioId, filePath);
      await fetchFiles(currentPath);
      showNotification("success", "File deleted successfully");
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to delete file"
      );
    }
  };

  const handleUnzip = async (filePath: string, currentPath: string = '') => {
    try {
      const response = await fetch(
        `/api/scenarios/${scenarioId}/data/unzip?path=${encodeURIComponent(filePath)}`,
        {
          method: "POST",
        }
      );

      if (!response.ok) {
        throw new Error(`Failed to unzip file: ${response.statusText}`);
      }

      await response.json();
      showNotification("success", "File unzipped successfully");
      await fetchFiles(currentPath);
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to unzip file"
      );
    }
  };

  return {
    files,
    loading,
    uploading,
    fetchFiles,
    handleUpload,
    handleDelete,
    handleUnzip,
  };
};