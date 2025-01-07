import FileViewer from './FileViewer';

interface FileViewerPanelProps {
  url: string | null;
  fileName: string | null;
  onClose: () => void;
}

export const FileViewerPanel: React.FC<FileViewerPanelProps> = ({
  url,
  fileName,
  onClose,
}) => {
  if (!url || !fileName) return null;

  return (
    <FileViewer
      url={url}
      fileName={fileName}
      open={true}
      onClose={onClose}
    />
  );
};