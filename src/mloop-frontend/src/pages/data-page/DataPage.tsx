import { useEffect, useRef, useState } from 'react';
import { useOutletContext, useSearchParams } from "react-router-dom";
import { Scenario } from "../../types/Scenario";
import { DataFile } from "../../types/DataFile";
import FileTable from "../../components/data/FileTable";
import { LoadingSpinner } from "../../components/common/LoadingSpinner";
import { DataHeader } from "./components/DataHeader";
import { DataBreadcrumbs } from "./components/DataBreadcrumbs";
import { EmptyDataState } from "./components/EmptyDataState";
import { FileViewerPanel } from "./components/FileViewerPanel";
import { useDataFiles } from "./hooks/useDataFiles";

type ScenarioContextType = {
  scenario: Scenario;
};

export const DataPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  const [searchParams, setSearchParams] = useSearchParams();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<{
    url: string;
    name: string;
  } | null>(null);

  const currentPath = searchParams.get("path") || "";

  const {
    files,
    loading,
    uploading,
    fetchFiles,
    handleUpload,
    handleDelete,
    handleUnzip,
  } = useDataFiles(scenario.scenarioId);

  useEffect(() => {
    fetchFiles(currentPath);
  }, [fetchFiles, currentPath]);

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files;
    if (!files) return;
    handleUpload(files, currentPath);
    if (event.target) event.target.value = '';
  };

  const handleNavigate = (path: string) => {
    setSearchParams(path ? { path } : {});
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

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <div className="p-6">
      <input
        ref={fileInputRef}
        type="file"
        id="fileUpload"
        className="hidden"
        onChange={handleFileUpload}
        multiple
        accept=".tsv, .csv, .xlsx, .zip"
        disabled={uploading}
      />

      <DataHeader 
        onUpload={() => fileInputRef.current?.click()} 
        uploading={uploading} 
      />
      
      <DataBreadcrumbs 
        currentPath={currentPath} 
        onNavigate={handleNavigate} 
      />

      {files.length === 0 ? (
        <EmptyDataState onUpload={() => fileInputRef.current?.click()} />
      ) : (
        <div className="bg-white rounded-lg">
          <FileTable
            files={files}
            onDelete={(path) => handleDelete(path, currentPath)}
            onUnzip={(path) => handleUnzip(path, currentPath)}
            onView={handleView}
            onNavigate={handleNavigate}
            onDownload={handleDownload}
          />
        </div>
      )}

      <FileViewerPanel
        url={selectedFile?.url ?? null}
        fileName={selectedFile?.name ?? null}
        onClose={() => setSelectedFile(null)}
      />
    </div>
  );
};