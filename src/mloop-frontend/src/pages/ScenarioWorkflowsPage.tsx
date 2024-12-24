import { useState, useEffect } from 'react';
import { useOutletContext } from 'react-router-dom';
import Editor from '@monaco-editor/react';
import { 
  SlButton, 
  SlIcon, 
  SlAlert,
  SlTab,
  SlTabGroup,
  SlTabPanel,
} from '@shoelace-style/shoelace/dist/react';
import { Scenario } from '../types/scenarios';

type ScenarioContextType = {
  scenario: Scenario;
};

const defaultTrainConfig = ``;

const defaultPredictConfig = ``;

const editorOptions = {
  minimap: { enabled: false },
  scrollBeyondLastLine: false,
  lineNumbers: 'on' as const,
  glyphMargin: false,
  folding: true,
  lineDecorationsWidth: 0,
  lineNumbersMinChars: 3,
  formatOnPaste: true,
  formatOnType: true,
  automaticLayout: true,
} as const;

export const ScenarioWorkflowsPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  const [activeTab, setActiveTab] = useState('train');
  const [trainConfig, setTrainConfig] = useState(defaultTrainConfig);
  const [predictConfig, setPredictConfig] = useState(defaultPredictConfig);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Load existing workflows
  useEffect(() => {
    const fetchWorkflows = async () => {
      try {
        const trainResponse = await fetch(`/api/scenarios/${scenario.scenarioId}/workflows/train`);
        if (trainResponse.ok) {
          const trainData = await trainResponse.text();
          setTrainConfig(trainData);
        }

        const predictResponse = await fetch(`/api/scenarios/${scenario.scenarioId}/workflows/predict`);
        if (predictResponse.ok) {
          const predictData = await predictResponse.text();
          setPredictConfig(predictData);
        }
      } catch (err) {
        console.error('Error loading workflows:', err);
      }
    };

    fetchWorkflows();
  }, [scenario.scenarioId]);

  const handleSave = async (type: 'train' | 'predict') => {
    setLoading(true);
    setError(null);
    
    try {
      const config = type === 'train' ? trainConfig : predictConfig;
      const response = await fetch(`/api/scenarios/${scenario.scenarioId}/workflows/${type}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'text/yaml',
        },
        body: config,
      });

      if (!response.ok) {
        throw new Error(`Failed to update ${type} workflow`);
      }

      setSuccessMessage(`${type.charAt(0).toUpperCase() + type.slice(1)} workflow updated successfully`);
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : `Failed to update ${type} workflow`);
    } finally {
      setLoading(false);
    }
  };

  const handleReset = (type: 'train' | 'predict') => {
    if (type === 'train') {
      setTrainConfig(defaultTrainConfig);
    } else {
      setPredictConfig(defaultPredictConfig);
    }
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-4">Workflows</h2>
        <p className="text-gray-600">
          Configure your machine learning workflows for training and prediction.
        </p>
      </div>

      {error && (
        <SlAlert variant="danger" className="mb-4">
          {error}
        </SlAlert>
      )}

      {successMessage && (
        <SlAlert variant="success" className="mb-4">
          {successMessage}
        </SlAlert>
      )}

      <SlTabGroup>
        <SlTab slot="nav" panel="train" active={activeTab === 'train'} onClick={() => setActiveTab('train')}>
          Training Workflow
        </SlTab>
        <SlTab slot="nav" panel="predict" active={activeTab === 'predict'} onClick={() => setActiveTab('predict')}>
          Prediction Workflow
        </SlTab>

        <SlTabPanel name="train">
          <div className="mt-4">
            <div className="mb-4 flex justify-between items-center">
              <h3 className="text-lg font-medium">Training Configuration</h3>
              <div className="space-x-2">
                <SlButton 
                  size="small"
                  variant="default" 
                  onClick={() => handleReset('train')}
                >
                  <SlIcon slot="prefix" name="arrow-counterclockwise" />
                  Reset
                </SlButton>
                <SlButton 
                  size="small"
                  variant="primary" 
                  onClick={() => handleSave('train')}
                  loading={loading && activeTab === 'train'}
                >
                  <SlIcon slot="prefix" name="save" />
                  Save
                </SlButton>
              </div>
            </div>
            <div className="border rounded-md overflow-hidden">
              <Editor
                height="400px"
                defaultLanguage="yaml"
                value={trainConfig}
                onChange={(value) => value && setTrainConfig(value)}
                theme="vs-light"
                options={editorOptions}
              />
            </div>
          </div>
        </SlTabPanel>

        <SlTabPanel name="predict">
          <div className="mt-4">
            <div className="mb-4 flex justify-between items-center">
              <h3 className="text-lg font-medium">Prediction Configuration</h3>
              <div className="space-x-2">
                <SlButton 
                  size="small"
                  variant="default" 
                  onClick={() => handleReset('predict')}
                >
                  <SlIcon slot="prefix" name="arrow-counterclockwise" />
                  Reset
                </SlButton>
                <SlButton 
                  size="small"
                  variant="primary" 
                  onClick={() => handleSave('predict')}
                  loading={loading && activeTab === 'predict'}
                >
                  <SlIcon slot="prefix" name="save" />
                  Save
                </SlButton>
              </div>
            </div>
            <div className="border rounded-md overflow-hidden">
              <Editor
                height="400px"
                defaultLanguage="yaml"
                value={predictConfig}
                onChange={(value) => value && setPredictConfig(value)}
                theme="vs-light"
                options={editorOptions}
              />
            </div>
          </div>
        </SlTabPanel>
      </SlTabGroup>
    </div>
  );
};

export default ScenarioWorkflowsPage;