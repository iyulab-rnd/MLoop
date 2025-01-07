import { useParams } from 'react-router-dom';
import { SlAlert } from '@shoelace-style/shoelace/dist/react';
import { ImageUploadPanel } from './components/ImageUploadPanel';
import { ResultPanel } from './components/ResultPanel';
import { TextInputPanel } from './components/TextInputPanel';
import { usePrediction } from './hooks/usePrediction';
import { BackButton } from '../components/BackButton';
import { useModelInfo } from '../../hooks/useModelInfo';
import { useNotification } from '../../hooks/useNotification';
import { useState, useEffect } from 'react';
import { scenarioApi } from '../../api/scenarios';

export const PredictPage = () => {
  const { scenarioId, modelId } = useParams();
  const { model, loading } = useModelInfo(scenarioId, modelId);
  const { showNotification } = useNotification();

  // 이미지 분류 관련 훅 및 상태
  const {
    predicting,
    result,
    error,
    setError,
    handleImageUpload
  } = usePrediction(scenarioId);

  // 비이미지 분류용 상태 및 변수
  const [input, setInput] = useState<string>('');
  const [textPredicting, setTextPredicting] = useState(false);
  const [textResult, setTextResult] = useState<string>('');
  const [textError, setTextError] = useState<string | null>(null);
  const [textPollingInterval, setTextPollingInterval] = useState<number | null>(null);

  useEffect(() => {
    return () => {
      if (textPollingInterval) {
        clearInterval(textPollingInterval);
      }
    };
  }, [textPollingInterval]);

  const formatResult = (response: any): string => {
    if (typeof response === 'string') {
      let formattedResult = response.replace(/^"+|"+$/g, '');
      formattedResult = formattedResult
        .replace(/\\r/g, '\r')
        .replace(/\\n/g, '\n')
        .replace(/\\t/g, '\t')
        .replace(/\\"/g, '"');
      return formattedResult;
    }
    return JSON.stringify(response, null, 2);
  };

  const startTextPolling = (pid: string) => {
    const interval = window.setInterval(async () => {
      try {
        const response = await scenarioApi.getPredictionResult(scenarioId!, pid);
        if (typeof response === 'object' && 'status' in response) {
          return;
        }
        clearInterval(interval);
        setTextPollingInterval(null);
        setTextPredicting(false);
        setTextResult(formatResult(response));
      } catch (error) {
        console.error(error);
        clearInterval(interval);
        setTextPollingInterval(null);
        setTextPredicting(false);
        showNotification('danger', 'Failed to get prediction result');
      }
    }, 2000);
    setTextPollingInterval(interval);
  };

  const preprocessInput = (rawInput: string): string => {
    let cleanInput = rawInput.replace(/^["']|["']$/g, '');
    cleanInput = cleanInput.replace(/^\ufeff/, '');
    cleanInput = cleanInput.replace(/\\r/g, '\r')
                           .replace(/\\n/g, '\n')
                           .replace(/\\t/g, '\t')
                           .replace(/\\\"/g, '"');
    const lines = cleanInput.split(/\r?\n/);
    const firstLine = lines[0];
    if (firstLine.includes('\t')) {
      return cleanInput;
    }
    if (firstLine.includes(',')) {
      return lines.map(line => {
        const values = [];
        let currentValue = '';
        let inQuotes = false;
        for (let i = 0; i < line.length; i++) {
          const char = line[i];
          if (char === '"') {
            if (inQuotes && line[i + 1] === '"') {
              currentValue += '"';
              i++;
            } else {
              inQuotes = !inQuotes;
            }
          } else if (char === ',' && !inQuotes) {
            values.push(currentValue.trim());
            currentValue = '';
          } else {
            currentValue += char;
          }
        }
        values.push(currentValue.trim());
        return values
          .map(value => value.replace(/^["']|["']$/g, ''))
          .join('\t');
      }).join('\n');
    }
    setTextError('Input must be in either TSV or CSV format');
    return cleanInput;
  };

  const handlePredict = async () => {
    try {
      setTextError(null);
      setTextPredicting(true);
      setTextResult('');
      
      const processedInput = preprocessInput(input);
      if (textError) {
        setTextPredicting(false);
        return;
      }
      
      const response = await scenarioApi.predict(scenarioId!, processedInput);
      startTextPolling(response.predictionId);
      
    } catch (err) {
      setTextPredicting(false);
      const errorMessage = err instanceof Error ? err.message : 'Prediction failed';
      showNotification('danger', errorMessage);
      setTextError(errorMessage);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!model) {
    return (
      <div className="p-6">
        <SlAlert variant="danger">Failed to load model information</SlAlert>
      </div>
    );
  }

  if (model.mlType === 'image-classification') {
    return (
      <div className="p-6">
        <div className="mb-6">
          <BackButton scenarioId={scenarioId!} />
          <h1 className="text-2xl font-bold mb-4">Image Classification</h1>
          <p className="text-gray-600">Upload images to get predictions</p>
        </div>

        {error && (
          <SlAlert variant="danger" className="mb-4" closable onSlAfterHide={() => setError(null)}>
            {error}
          </SlAlert>
        )}

        <div className="grid grid-cols-2 gap-6">
          <ImageUploadPanel
            onUpload={(e) => handleImageUpload(e, modelId)}
            predicting={predicting}
          />
          <ResultPanel
            predicting={predicting}
            result={result}
          />
        </div>
      </div>
    );
  } else {
    return (
      <div className="p-6">
        <div className="mb-6">
          <BackButton scenarioId={scenarioId!} />
          <h1 className="text-2xl font-bold mb-4">Predict</h1>
          <p className="text-gray-600">Enter your input data in TSV or CSV format</p>
        </div>

        {textError && (
          <SlAlert variant="danger" className="mb-4" closable onSlAfterHide={() => setTextError(null)}>
            {textError}
          </SlAlert>
        )}

        <div className="grid grid-cols-2 gap-6">
          <TextInputPanel 
            input={input}
            setInput={setInput}
            onPredict={handlePredict}
            predicting={textPredicting}
          />
          <ResultPanel
            predicting={textPredicting}
            result={textResult}
          />
        </div>
      </div>
    );
  }
};
