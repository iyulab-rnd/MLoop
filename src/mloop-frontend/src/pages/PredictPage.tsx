import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import Editor from '@monaco-editor/react';
import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';
import { scenarioApi } from '../api/scenarios';
import { useNotification } from "../hooks/useNotification";

const defaultInput = ``;

export const PredictPage = () => {
  const { scenarioId } = useParams();
  const { showNotification } = useNotification();
  const [input, setInput] = useState(defaultInput);
  const [predicting, setPredicting] = useState(false);
  const [result, setResult] = useState<string>('');
  const [pollingInterval, setPollingInterval] = useState<number | null>(null);

  useEffect(() => {
    return () => {
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
    };
  }, [pollingInterval]);

  const startPolling = (pid: string) => {
    const interval = setInterval(async () => {
      try {
        const response = await scenarioApi.getPredictionResult(scenarioId!, pid);
        
        if (typeof response === 'object' && 'status' in response) {
          // Still processing
          return;
        }
        
        // Prediction completed - format the result as text
        const resultText = JSON.stringify(response, null, 2);
        clearInterval(interval);
        setPollingInterval(null);
        setPredicting(false);
        setResult(resultText);
      } catch (error) {
        console.error(error);
        clearInterval(interval);
        setPollingInterval(null);
        setPredicting(false);
        showNotification('danger', 'Failed to get prediction result');
      }
    }, 2000);

    setPollingInterval(interval);
  };

  const handlePredict = async () => {
    try {
      setPredicting(true);
      setResult('');
      
      const response = await scenarioApi.predict(scenarioId!, input);
      startPolling(response.predictionId);
      
    } catch (err) {
      setPredicting(false);
      showNotification('danger', err instanceof Error ? err.message : 'Prediction failed');
    }
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold mb-4">Predict</h1>
        <p className="text-gray-600">Enter your input data in TSV/CSV format</p>
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Input Editor Panel */}
        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="text-lg font-medium">Input Data (CSV/TSV)</h2>
            <SlButton 
              variant="primary" 
              onClick={handlePredict}
              loading={predicting}
            >
              <SlIcon slot="prefix" name="play-fill" />
              Predict
            </SlButton>
          </div>
          <div className="border rounded-lg overflow-hidden">
            <Editor
              height="600px"
              defaultLanguage="text"
              value={input}
              onChange={(value) => value && setInput(value)}
              options={{
                minimap: { enabled: false },
                lineNumbers: 'on',
                scrollBeyondLastLine: false,
              }}
            />
          </div>
        </div>

        {/* Result Editor Panel */}
        <div>
          <h2 className="text-lg font-medium mb-4">Results</h2>
          <div className="border rounded-lg overflow-hidden h-[600px]">
            {predicting ? (
              <div className="flex items-center justify-center h-full bg-white">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
                  <p className="text-gray-600">Processing prediction...</p>
                </div>
              </div>
            ) : (
              <Editor
                height="600px"
                defaultLanguage="json"
                value={result || "// No results yet. Click Predict to start prediction."}
                options={{
                  readOnly: true,
                  minimap: { enabled: false },
                  lineNumbers: 'on',
                  scrollBeyondLastLine: false,
                }}
              />
            )}
          </div>
        </div>
      </div>
    </div>
  );
};