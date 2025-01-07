import { useState, useCallback, useEffect } from "react";
import { useNotification } from "../../../hooks/useNotification";
import { predictionsApi } from "../../../api/predictions";
import { PredictionFile } from "../../../types/Prediction";

export const usePredictionFiles = (
  scenarioId: string | undefined,
  predictionId: string | undefined
) => {
  const { showNotification } = useNotification();
  const [files, setFiles] = useState<PredictionFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedFile, setSelectedFile] = useState<{
    url: string;
    name: string;
  } | null>(null);

  const fetchFiles = useCallback(async () => {
    if (!scenarioId || !predictionId) return;

    try {
      setLoading(true);
      const data = await predictionsApi.listFiles(scenarioId, predictionId);
      setFiles(data);
    } catch (error) {
      console.error("Error fetching files:", error);
      showNotification(
        "danger",
        error instanceof Error ? error.message : "Failed to load files"
      );
    } finally {
      setLoading(false);
    }
  }, [scenarioId, predictionId, showNotification]);

  useEffect(() => {
    fetchFiles();
  }, [fetchFiles]);

  const handleFileAction = useCallback(
    (file: PredictionFile) => {
      if (!scenarioId || !predictionId) return;

      const fileUrl = `/api/scenarios/${scenarioId}/predictions/${predictionId}/files/${file.path}`;

      if (isPreviewable(file.name)) {
        setSelectedFile({ url: fileUrl, name: file.name });
      } else {
        const a = document.createElement("a");
        a.href = fileUrl;
        a.download = file.name;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
      }
    },
    [scenarioId, predictionId]
  );

  const isPreviewable = (filename: string) => {
    const ext = filename.split(".").pop()?.toLowerCase();
    return ext === "csv" || ext === "tsv";
  };

  return {
    files,
    loading,
    selectedFile,
    setSelectedFile,
    fetchFiles,
    handleFileAction,
  };
};
