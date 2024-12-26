import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import Editor from '@monaco-editor/react';
import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';
import { scenarioApi } from '../api/scenarios';
import { useNotification } from '../contexts/NotificationContext';

const defaultInput = ``;

export const PredictPage = () => {
  const { scenarioId, modelId } = useParams();
  const { showNotification } = useNotification();
  const [input, setInput] = useState(defaultInput);
  const [predicting, setPredicting] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [predictionId, setPredictionId] = useState<string | null>(null);
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
      
      const { predictionId } = await scenarioApi.predict(scenarioId!, input);
      setPredictionId(predictionId);
      startPolling(predictionId);
      
    } catch (error) {
      setPredicting(false);
      showNotification('danger', error instanceof Error ? error.message : 'Prediction failed');
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
                  <div className="bg-gray-50 p-4 rounded-lg">
                    <h3 className="font-medium text-sm text-gray-500 mb-2">Top 1</h3>
                    <p className="font-medium">{result.Top1}</p>
                    <p className="text-sm text-gray-600">{(result.Top1Score * 100).toFixed(2)}%</p>
                  </div>
                  <div className="bg-gray-50 p-4 rounded-lg">
                    <h3 className="font-medium text-sm text-gray-500 mb-2">Top 2</h3>
                    <p className="font-medium">{result.Top2}</p>
                    <p className="text-sm text-gray-600">{(result.Top2Score * 100).toFixed(2)}%</p>
                  </div>
                  <div className="bg-gray-50 p-4 rounded-lg">
                    <h3 className="font-medium text-sm text-gray-500 mb-2">Top 3</h3>
                    <p className="font-medium">{result.Top3}</p>
                    <p className="text-sm text-gray-600">{(result.Top3Score * 100).toFixed(2)}%</p>
                  </div>
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