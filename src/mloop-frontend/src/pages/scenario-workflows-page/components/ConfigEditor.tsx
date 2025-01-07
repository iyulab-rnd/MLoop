import Editor from "@monaco-editor/react";
import {
  SlButton,
  SlIcon,
  SlSelect,
  SlOption,
} from "@shoelace-style/shoelace/dist/react";

interface ConfigEditorProps {
  label: string;
  value: string;
  onChange: (value: string | undefined) => void;
  onSave: () => void;
  onReset: () => void;
  templates: string[];
  onTemplateChange: (e: any) => void;
  isLoading: boolean;
}

export const ConfigEditor: React.FC<ConfigEditorProps> = ({
  label,
  value,
  onChange,
  onSave,
  onReset,
  templates,
  onTemplateChange,
  isLoading,
}) => {
  return (
    <div className="mt-4">
      <div className="mb-4 flex justify-between items-center">
        <div className="flex items-center gap-4">
          <h3 className="text-lg font-medium">{label}</h3>
          {templates.length > 0 && (
            <SlSelect
              size="small"
              placeholder="Select a template..."
              onSlChange={onTemplateChange}
            >
              {templates.map((template) => (
                <SlOption key={template} value={template}>
                  {template.replace(".yaml", "")}
                </SlOption>
              ))}
            </SlSelect>
          )}
        </div>
        <div className="space-x-2">
          <SlButton size="small" variant="default" onClick={onReset}>
            <SlIcon slot="prefix" name="arrow-counterclockwise" />
            Reset
          </SlButton>
          <SlButton
            size="small"
            variant="primary"
            onClick={onSave}
            loading={isLoading}
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
          value={value}
          onChange={onChange}
          theme="vs-light"
          options={{
            minimap: { enabled: false },
            scrollBeyondLastLine: false,
            lineNumbers: "on",
            glyphMargin: false,
            folding: true,
            lineDecorationsWidth: 0,
            lineNumbersMinChars: 3,
            formatOnPaste: true,
            formatOnType: true,
            automaticLayout: true,
          }}
        />
      </div>
    </div>
  );
};
