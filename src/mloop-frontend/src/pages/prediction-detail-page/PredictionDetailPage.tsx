import { useParams } from "react-router-dom";
import { LoadingSpinner } from "../../components/common/LoadingSpinner";
import FileViewer from "../data-page/components/FileViewer";
import { PredictionHeader } from "./components/PredictionHeader";
import { EmptyFilesState } from "./components/EmptyFilesState";
import { FileList } from "./components/FileList";
import { usePredictionFiles } from "./hooks/usePredictionFiles";

export const PredictionDetailPage = () => {
  const { scenarioId, predictionId } = useParams();
  const {
    files,
    loading,
    selectedFile,
    setSelectedFile,
    fetchFiles,
    handleFileAction,
  } = usePredictionFiles(scenarioId, predictionId);

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <div className="p-6">
      <PredictionHeader scenarioId={scenarioId!} onRefresh={fetchFiles} />

      {files.length === 0 ? (
        <EmptyFilesState />
      ) : (
        <FileList files={files} onFileAction={handleFileAction} />
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
