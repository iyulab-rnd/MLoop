import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import Editor from '@monaco-editor/react';
import { SlButton, SlIcon, SlAlert } from '@shoelace-style/shoelace/dist/react';
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
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    return () => {
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
    };
  }, [pollingInterval]);

  const formatResult = (response: any): string => {
    if (typeof response === 'string') {
      // Remove surrounding quotes
      let formattedResult = response.replace(/^"|"$/g, '');
      // Convert escaped characters to actual characters
      formattedResult = formattedResult
        .replace(/\\r/g, '\r')
        .replace(/\\n/g, '\n')
        .replace(/\\t/g, '\t')
        .replace(/\\"/g, '"');
      return formattedResult;
    }
    // If response is an object, format it nicely
    return JSON.stringify(response, null, 2);
  };

  const startPolling = (pid: string) => {
    const interval = setInterval(async () => {
      try {
        const response = await scenarioApi.getPredictionResult(scenarioId!, pid);
        
        if (typeof response === 'object' && 'status' in response) {
          // Still processing
          return;
        }
        
        // Prediction completed - format the result
        const formattedResult = formatResult(response);
        clearInterval(interval);
        setPollingInterval(null);
        setPredicting(false);
        setResult(formattedResult);
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

  const preprocessInput = (rawInput: string): string => {
    // Remove surrounding quotes if they exist
    let cleanInput = rawInput.replace(/^["']|["']$/g, '');
    
    // Remove any BOM characters
    cleanInput = cleanInput.replace(/^\ufeff/, '');
    
    // Unescape special characters
    cleanInput = cleanInput.replace(/\\r/g, '\r')
                          .replace(/\\n/g, '\n')
                          .replace(/\\t/g, '\t')
                          .replace(/\\\"/g, '"');
    
    // Check if input is already properly formatted after unescaping
    const lines = cleanInput.split(/\r?\n/);
    const firstLine = lines[0];
    
    // If the first line contains a tab, treat as TSV
    if (firstLine.includes('\t')) {
      return cleanInput;
    }
    
    // If the first line contains commas, convert CSV to TSV
    if (firstLine.includes(',')) {
      return lines.map(line => {
        // Handle quoted values properly
        const values = [];
        let currentValue = '';
        let inQuotes = false;
        
        for (let i = 0; i < line.length; i++) {
          const char = line[i];
          
          if (char === '"') {
            if (inQuotes && line[i + 1] === '"') {
              // Handle escaped quotes
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
        
        // Convert to TSV line
        return values
          .map(value => value.replace(/^["']|["']$/g, ''))  // Remove surrounding quotes
          .join('\t');
      }).join('\n');
    }
    
    // If neither tab nor comma is found, show error
    setError('Input must be in either TSV or CSV format');
    return cleanInput;
  };

  const handlePredict = async () => {
    try {
      setError(null);
      setPredicting(true);
      setResult('');
      
      const processedInput = preprocessInput(input);
      if (error) {
        setPredicting(false);
        return;
      }
      
      const response = await scenarioApi.predict(scenarioId!, processedInput);
      startPolling(response.predictionId);
      
    } catch (err) {
      setPredicting(false);
      const errorMessage = err instanceof Error ? err.message : 'Prediction failed';
      showNotification('danger', errorMessage);
      setError(errorMessage);
    }
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold mb-4">Predict</h1>
        <p className="text-gray-600">Enter your input data in TSV or CSV format</p>
      </div>

      {error && (
        <SlAlert variant="danger" className="mb-4" closable onSlAfterHide={() => setError(null)}>
          {error}
        </SlAlert>
      )}

      <div className="grid grid-cols-2 gap-6">
        {/* Input Editor Panel */}
        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="text-lg font-medium">Input Data (TSV/CSV)</h2>
            <SlButton 
              variant="primary" 
              onClick={handlePredict}
              loading={predicting}
              disabled={!input.trim()}
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
                wordWrap: 'on'
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
                defaultLanguage="text"
                value={result || "// No results yet. Click Predict to start prediction."}
                options={{
                  readOnly: true,
                  minimap: { enabled: false },
                  lineNumbers: 'on',
                  scrollBeyondLastLine: false,
                  wordWrap: 'on'
                }}
              />
            )}
          </div>
        </div>
      </div>
    </div>
  );
};