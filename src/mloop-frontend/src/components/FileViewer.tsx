import React, { useState, useEffect } from 'react';
import Editor from '@monaco-editor/react';
import { SlButton, SlIcon, SlDialog } from '@shoelace-style/shoelace/dist/react';

const FileViewer = ({ 
  url, 
  fileName, 
  open, 
  onClose 
}: { 
  url: string; 
  fileName: string; 
  open: boolean; 
  onClose: () => void;
}) => {
  const [content, setContent] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const response = await fetch(url);
        const text = await response.text();
        setContent(text);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load file');
      } finally {
        setLoading(false);
      }
    };

    if (open) {
      fetchData();
    }
  }, [url, open]);

  const handleCopy = () => {
    navigator.clipboard.writeText(content);
  };

  const handleDownload = () => {
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  // Determine language based on file extension
  const getLanguage = (filename: string) => {
    const ext = filename.split('.').pop()?.toLowerCase();
    if (ext === 'csv') return 'csv';
    if (ext === 'tsv') return 'text';
    return 'text';
  };

  return (
    <SlDialog 
      label={fileName}
      open={open}
      onSlAfterHide={onClose}
      className="file-viewer-dialog"
      style={{
        '--width': 'calc(100% - 64px)',
        maxWidth: 'calc(100% - 64px)',
        '--height': 'calc(100vh - 64px)',
        maxHeight: 'calc(100vh - 64px)',
      } as React.CSSProperties}
    >
      <div className="h-[calc(100vh-200px)]">
        {loading ? (
          <div className="flex items-center justify-center h-48">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          </div>
        ) : error ? (
          <div className="text-red-600 p-4">{error}</div>
        ) : (
          <Editor
            height="100%"
            language={getLanguage(fileName)}
            value={content}
            options={{
              readOnly: true,
              minimap: { enabled: true },
              lineNumbers: 'on',
              scrollBeyondLastLine: false,
              wordWrap: 'on',
              wrappingIndent: 'indent'
            }}
          />
        )}
      </div>
      <div slot="footer" className="flex justify-end gap-2">
        <SlButton variant="default" size="small" onClick={handleCopy}>
          <SlIcon slot="prefix" name="clipboard" />
          Copy
        </SlButton>
        <SlButton variant="default" size="small" onClick={handleDownload}>
          <SlIcon slot="prefix" name="download" />
          Download
        </SlButton>
        <SlButton variant="primary" size="small" onClick={onClose}>
          Close
        </SlButton>
      </div>
    </SlDialog>
  );
};

export default FileViewer;