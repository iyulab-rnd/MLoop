// src/pages/DataPage.tsx
import React, { useState, useEffect, useCallback } from "react";
import { useOutletContext, useSearchParams } from "react-router-dom";
import {
  SlButton,
  SlIcon,
  SlBreadcrumb,
  SlBreadcrumbItem,
} from "@shoelace-style/shoelace/dist/react";
import { Scenario } from "../types/Scenario";
import { DataFile } from "../types/DataFile";
import { scenarioApi } from "../api/scenarios";
import { useNotification } from "../hooks/useNotification";
import FileViewer from "../components/FileViewer";
import FileTable from "../components/data/FileTable";

type ScenarioContextType = {
  scenario: Scenario;
};

export const DataPage = () => {
  const { showNotification } = useNotification();
  const { scenario } = useOutletContext<ScenarioContextType>();
  const [searchParams, setSearchParams] = useSearchParams();
  const [files, setFiles] = useState<DataFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<{
    url: string;
    name: string;
  } | null>(null);

  const currentPath = searchParams.get("path") || "";

  const fetchFiles = useCallback(async () => {
    try {
      setLoading(true);
      const data = await scenarioApi.listFiles(
        scenario.scenarioId,
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
  }, [scenario.scenarioId, currentPath, showNotification]);

  useEffect(() => {
    fetchFiles();
  }, [fetchFiles]);

  const handleUnzip = async (filePath: string) => {
    try {
      const response = await fetch(
        `/api/scenarios/${
          scenario.scenarioId
        }/data/unzip?path=${encodeURIComponent(filePath)}`,
        {
          method: "POST",
        }
      );

      if (!response.ok) {
        throw new Error(`Failed to unzip file: ${response.statusText}`);
      }

      await response.json();
      showNotification("success", "File unzipped successfully");
      await fetchFiles();
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to unzip file"
      );
    }
  };

  const handleFilesUpload = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const selectedFiles = event.target.files;
    if (!selectedFiles || selectedFiles.length === 0) return;

    const MAX_SIZE = {
      zip: 500 * 1024 * 1024, // 500MB for zip files
      default: 200 * 1024 * 1024, // 200MB for other files
    };

    for (const file of selectedFiles) {
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
    Array.from(selectedFiles).forEach((file) => {
      formData.append("files", file);
    });

    try {
      await fetch(
        `/api/scenarios/${scenario.scenarioId}/data${
          currentPath ? `?path=${encodeURIComponent(currentPath)}` : ""
        }`,
        {
          method: "POST",
          body: formData,
        }
      );

      await fetchFiles();
      showNotification(
        "success",
        `${selectedFiles.length} file(s) uploaded successfully`
      );
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to upload files"
      );
    } finally {
      setUploading(false);
      event.target.value = "";
    }
  };

  const handleDelete = async (filePath: string) => {
    try {
      await scenarioApi.deleteFile(scenario.scenarioId, filePath);
      await fetchFiles();
      showNotification("success", "File deleted successfully");
    } catch (error) {
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to delete file"
      );
    }
  };

  const handleNavigate = (path: string) => {
    setSearchParams({ path });
  };

  const handleView = (file: DataFile) => {
    const fileUrl = `/api/scenarios/${scenario.scenarioId}/data/${file.path}`;
    setSelectedFile({ url: fileUrl, name: file.name });
  };

  const handleDownload = (filePath: string, fileName: string) => {
    const downloadUrl = `/api/scenarios/${scenario.scenarioId}/data/${filePath}`;
    const link = document.createElement("a");
    link.href = downloadUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const renderBreadcrumbs = () => {
    if (!currentPath) return null;

    const pathParts = currentPath.split("/");
    return (
      <SlBreadcrumb className="mb-4">
        <SlBreadcrumbItem onClick={() => setSearchParams({})}>
          Root
        </SlBreadcrumbItem>
        {pathParts.map((part, index) => {
          const path = pathParts.slice(0, index + 1).join("/");
          return (
            <SlBreadcrumbItem
              key={path}
              onClick={() => setSearchParams({ path })}
            >
              {part}
            </SlBreadcrumbItem>
          );
        })}
      </SlBreadcrumb>
    );
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
      <div className="mb-6">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-2xl font-semibold">Dataset List</h2>
            <p className="text-gray-600 mt-1">
              View and manage datasets for this scenario
            </p>
          </div>
          <div>
            <input
              type="file"
              id="fileUpload"
              className="hidden"
              onChange={handleFilesUpload}
              multiple
              accept=".tsv, .csv, .xlsx, .zip"
              disabled={uploading}
            />
            <SlButton
              variant="primary"
              onClick={() => document.getElementById("fileUpload")?.click()}
              loading={uploading}
            >
              <SlIcon slot="prefix" name="upload" />
              Upload Files
            </SlButton>
          </div>
        </div>
        {renderBreadcrumbs()}
      </div>

      {files.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
          <p>No datasets available in this directory.</p>
          <p className="mt-2">Click the button above to upload your files.</p>
        </div>
      ) : (
        <div className="bg-white rounded-lg">
          <FileTable
            files={files}
            onDelete={handleDelete}
            onUnzip={handleUnzip}
            onView={handleView}
            onNavigate={handleNavigate}
            onDownload={handleDownload} // Download 핸들러 전달
          />
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
};
