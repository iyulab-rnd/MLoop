import Editor from '@monaco-editor/react';
import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';
import React from 'react';

interface TextInputPanelProps {
  input: string;
  setInput: (value: string) => void;
  onPredict: () => void;
  predicting: boolean;
}

export const TextInputPanel: React.FC<TextInputPanelProps> = ({ input, setInput, onPredict, predicting }) => {
  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-lg font-medium">Input Data (TSV/CSV)</h2>
        <SlButton 
          variant="primary" 
          onClick={onPredict}
          loading={predicting}
          disabled={!input.trim()}
        >
          <SlIcon slot="prefix" name="play-fill" />
          Predict
        </SlButton>
      </div>
      <div className="border rounded-lg overflow-hidden">
        <Editor
          height="400px"
          defaultLanguage="text"
          value={input}
          onChange={(value) => {
            if (value !== undefined) {
              setInput(value);
            }
          }}          
          options={{
            minimap: { enabled: false },
            lineNumbers: 'on',
            scrollBeyondLastLine: false,
            wordWrap: 'on'
          }}
        />
      </div>
    </div>
  );
};
