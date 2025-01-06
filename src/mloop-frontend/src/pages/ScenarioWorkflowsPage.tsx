import React, { useState, useEffect, useCallback } from 'react';
import { useOutletContext } from 'react-router-dom';
import Editor from '@monaco-editor/react';
import { 
  SlButton, 
  SlIcon, 
  SlAlert,
  SlTab,
  SlTabGroup,
  SlTabPanel,
  SlSelect,
  SlOption,
} from '@shoelace-style/shoelace/dist/react';
import { Scenario } from '../types/Scenario';
import { scenarioApi } from '../api/scenarios';
import { useNotification } from "../hooks/useNotification";
import { SlChangeEvent } from '@shoelace-style/shoelace';

type ScenarioContextType = {
  scenario: Scenario;
};

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
  const { showNotification } = useNotification();
  const [activeTab, setActiveTab] = useState('train');
  const [trainConfig, setTrainConfig] = useState('');
  const [predictConfig, setPredictConfig] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  
  const [trainTemplates, setTrainTemplates] = useState<string[]>([]);
  const [predictTemplates, setPredictTemplates] = useState<string[]>([]);
  
  // Controlled select values
  const [trainSelectValue, setTrainSelectValue] = useState('');
  const [predictSelectValue, setPredictSelectValue] = useState('');

  useEffect(() => {
    const loadTemplateList = async () => {
      try {
        // Load train templates
        const trainResponse = await fetch('/templates/train/list.txt');
        if (!trainResponse.ok) {
          throw new Error('Failed to load training templates list');
        }
        const trainList = await trainResponse.text();
        const trainTemplateNames = trainList.split('\n')
          .map(line => line.trim())
          .filter(line => line && line.endsWith('.yaml'));
        setTrainTemplates(trainTemplateNames);

        // Attempt to load predict templates
        try {
          const predictResponse = await fetch('/templates/predict/list.txt');
          if (predictResponse.ok) {
            const predictList = await predictResponse.text();
            const predictTemplateNames = predictList.split('\n')
              .map(line => line.trim())
              .filter(line => line && line.endsWith('.yaml'));
            setPredictTemplates(predictTemplateNames);
          }
        } catch (err) {
          console.log('Predict templates list not available');
        }
      } catch (err) {
        console.error('Error loading template list:', err);
        showNotification('danger', 'Failed to load template list.');
      }
    };

    loadTemplateList();
  }, [showNotification]);

  // Load existing workflows
  useEffect(() => {
    const fetchWorkflows = async () => {
      try {
        setLoading(true);
        setError(null);

        const trainData = await scenarioApi.getWorkflow(scenario.scenarioId, 'train');
        if (trainData) {
          setTrainConfig(trainData);
        }

        const predictData = await scenarioApi.getWorkflow(scenario.scenarioId, 'predict');
        if (predictData) {
          setPredictConfig(predictData);
        }
      } catch (err) {
        console.error('Error loading workflows:', err);
        setError(err instanceof Error ? err.message : 'Failed to load workflows');
      } finally {
        setLoading(false);
      }
    };

    fetchWorkflows();
  }, [scenario.scenarioId]);

  const loadTemplate = useCallback(async (type: 'train' | 'predict', templateName: string) => {
    try {
      const response = await fetch(`/templates/${type}/${templateName}`);
      if (!response.ok) {
        throw new Error(`Failed to load ${type} template`);
      }
      
      const content = await response.text();
      if (type === 'train') {
        setTrainConfig(content);
      } else {
        setPredictConfig(content);
      }
      
      showNotification('success', 'Template loaded successfully.');
    } catch (err) {
      console.error('Error loading template:', err);
      showNotification('danger', 'Failed to load template.');
    }
  }, [showNotification]);

  const handleSave = async (type: 'train' | 'predict') => {
    setLoading(true);
    setError(null);
    
    try {
      const config = type === 'train' ? trainConfig : predictConfig;
      
      // Validate YAML
      try {
        const jsyaml = await import('js-yaml');
        jsyaml.load(config);
      } catch (yamlError) {
        throw new Error(`Invalid YAML format: ${yamlError instanceof Error ? yamlError.message : 'Unknown error'}`);
      }
  
      await scenarioApi.saveWorkflow(scenario.scenarioId, type, config);
      
      setSuccessMessage(`${type.charAt(0).toUpperCase() + type.slice(1)} workflow updated successfully.`);
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err) {
      console.error('Error saving workflow:', err);
      setError(err instanceof Error ? err.message : `Failed to update ${type} workflow.`);
    } finally {
      setLoading(false);
    }
  };

  const handleReset = useCallback((type: 'train' | 'predict') => {
    if (type === 'train') {
      setTrainConfig('');
    } else {
      setPredictConfig('');
    }
  }, []);

  const handleTemplateChange = useCallback((type: 'train' | 'predict') => (e: SlChangeEvent) => {
    const template = e.detail.value || (e.target as HTMLInputElement).value || '';
    console.log(`Selected ${type} template:`, template);
    if (template) {
      loadTemplate(type, template);
      // Reset the select's value by updating state
      if (type === 'train') {
        setTrainSelectValue('');
      } else {
        setPredictSelectValue('');
      }
    }
  }, [loadTemplate]);

  return (
    <div className="p-6">
      <div className="mb-6">
        <h2 className="text-2xl font-semibold mb-4">Workflows</h2>
        <p className="text-gray-600">
          Configure your machine learning workflows for training and prediction.
          You can start with a template or create your own configuration.
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
        <SlTab 
          slot="nav" 
          panel="train" 
          active={activeTab === 'train'} 
          onClick={() => setActiveTab('train')}
        >
          Training Workflow
        </SlTab>
        <SlTab 
          slot="nav" 
          panel="predict" 
          active={activeTab === 'predict'} 
          onClick={() => setActiveTab('predict')}
        >
          Prediction Workflow
        </SlTab>

        <SlTabPanel name="train">
          <div className="mt-4">
            <div className="mb-4 flex justify-between items-center">
              <div className="flex items-center gap-4">
                <h3 className="text-lg font-medium">Training Configuration</h3>
                {trainTemplates.length > 0 && (
                  <SlSelect 
                    size="small"
                    placeholder="Select a template..."
                    onSlChange={handleTemplateChange('train')}
                    value={trainSelectValue} // Controlled value
                  >
                    {trainTemplates.map((template) => (
                      <SlOption key={template} value={template}>
                        {template.replace('.yaml', '')}
                      </SlOption>
                    ))}
                  </SlSelect>
                )}
              </div>
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
              <div className="flex items-center gap-4">
                <h3 className="text-lg font-medium">Prediction Configuration</h3>
                {predictTemplates.length > 0 && (
                  <SlSelect 
                    size="small"
                    placeholder="Select a template..."
                    onSlChange={handleTemplateChange('predict')}
                    value={predictSelectValue} // Controlled value
                  >
                    {predictTemplates.map((template) => (
                      <SlOption key={template} value={template}>
                        {template.replace('.yaml', '')}
                      </SlOption>
                    ))}
                  </SlSelect>
                )}
              </div>
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
