import { useState, useEffect } from 'react';
import { scenarioApi } from '../../../api/scenarios';
import { useNotification } from '../../../hooks/useNotification';

export const usePrediction = (scenarioId: string | undefined) => {
  const [predicting, setPredicting] = useState(false);
  const [result, setResult] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [pollingInterval, setPollingInterval] = useState<number>(0);
  const { showNotification } = useNotification();

  useEffect(() => {
    return () => {
      if (pollingInterval) {
        window.clearInterval(pollingInterval);
      }
    };
  }, [pollingInterval]);

  const startPolling = (pid: string) => {
    const interval = window.setInterval(async () => {
      try {
        const response = await scenarioApi.getPredictionResult(scenarioId!, pid);
        
        if (typeof response === 'object' && 'status' in response) {
          return;
        }
        
        window.clearInterval(interval);
        setPollingInterval(0);
        setPredicting(false);
        setResult(response as string);
      } catch (error) {
        console.error(error);
        window.clearInterval(interval);
        setPollingInterval(0);
        setPredicting(false);
        showNotification('danger', 'Failed to get prediction result');
      }
    }, 2000);

    setPollingInterval(interval);
  };

  const handleImageUpload = async (event: React.ChangeEvent<HTMLInputElement>, modelId?: string) => {
    const files = event.target.files;
    if (!files || files.length === 0) return;

    try {
      setPredicting(true);
      setResult('');
      setError(null);

      for (const file of files) {
        const formData = new FormData();
        formData.append('file', file);

        const response = await fetch(
          `/api/scenarios/${scenarioId}/predictions/image-classification${modelId ? `?modelId=${modelId}` : ''}`,
          {
            method: 'POST',
            body: formData,
          }
        );

        if (!response.ok) {
          throw new Error('Failed to upload image for prediction');
        }

        const data = await response.json();
        startPolling(data.predictionId);
      }
    } catch (err) {
      setPredicting(false);
      const errorMessage = err instanceof Error ? err.message : 'Prediction failed';
      showNotification('danger', errorMessage);
      setError(errorMessage);
    }
  };

  return {
    predicting,
    result,
    error,
    setError,
    handleImageUpload
  };
};
