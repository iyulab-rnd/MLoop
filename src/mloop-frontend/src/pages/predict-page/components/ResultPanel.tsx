interface ResultPanelProps {
  predicting: boolean;
  result: string;
}

export const ResultPanel: React.FC<ResultPanelProps> = ({
  predicting,
  result,
}) => {
  return (
    <div>
      <h2 className="text-lg font-medium mb-4">Results</h2>
      <div className="border rounded-lg h-[400px] bg-white p-4 overflow-auto">
        {predicting ? (
          <div className="flex items-center justify-center h-full">
            <div className="text-center">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
              <p className="text-gray-600">Processing prediction...</p>
            </div>
          </div>
        ) : result ? (
          <pre className="text-sm">{result}</pre>
        ) : (
          <div className="flex items-center justify-center h-full text-gray-500">
            Upload data to see prediction results
          </div>
        )}
      </div>
    </div>
  );
};
