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
  const [result, setResult] = useState<Record<string, string | number> | null>(null);
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
        
        if (response.status === 'processing') {
          // 계속 폴링
          return;
        }
        
        // CSV 응답인 경우 (예측 완료)
        clearInterval(interval);
        setPollingInterval(null);
        setPredicting(false);
        setResult(response);
      } catch (error) {
        console.error(error);
        clearInterval(interval);
        setPollingInterval(null);
        setPredicting(false);
        showNotification('danger', 'Failed to get prediction result');
      }
    }, 2000); // 2초마다 폴링

    setPollingInterval(interval);
  };

  const handlePredict = async () => {
    try {
      setPredicting(true);
      setResult(null);
      
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
        {/* 좌측 에디터 패널 */}
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

        {/* 우측 결과 패널 */}
        <div>
          <h2 className="text-lg font-medium mb-4">Results</h2>
          <div className="border rounded-lg p-4 h-[600px] overflow-auto bg-white">
            {predicting ? (
              <div className="flex items-center justify-center h-full">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
                  <p className="text-gray-600">Processing prediction...</p>
                </div>
              </div>
            ) : result ? (
              <div>
                <div className="grid grid-cols-3 gap-4">
                  {Object.entries(result)
                    .filter(([key]) => !key.includes('status'))
                    .reduce((acc: [string, string | number][], [, value], index) => {
                      if (index % 2 === 0) {
                        // label entry
                        const nextValue = Object.entries(result)[index + 1]?.[1];
                        if (nextValue !== undefined) {
                          acc.push([value.toString(), nextValue as number]);
                        }
                      }
                      return acc;
                    }, [])
                    .map(([label, score], index) => (
                      <div key={index} className="bg-gray-50 p-4 rounded-lg">
                        <h3 className="font-medium text-sm text-gray-500 mb-2">Top {index + 1}</h3>
                        <p className="font-medium">{label}</p>
                        <p className="text-sm text-gray-600">{(Number(score) * 100).toFixed(2)}%</p>
                      </div>
                    ))}
                </div>
              </div>
            ) : (
              <div className="flex items-center justify-center h-full text-gray-500">
                No results yet. Click Predict to start prediction.
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};