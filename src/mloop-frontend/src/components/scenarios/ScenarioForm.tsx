import React, { useCallback } from 'react';
import { SlInput, SlButton, SlSelect, SlOption, SlTextarea } from '@shoelace-style/shoelace/dist/react';
import { TagInput } from '../../components/common/TagInput';

export interface ScenarioFormData {
  name: string;
  mlType: string;
  description: string;
  tags: string[];
}

interface ScenarioFormProps {
  formData: ScenarioFormData;
  isSubmitting: boolean;
  error: string | null;
  onSubmit: (e: React.FormEvent) => Promise<void>;
  onChange: (data: ScenarioFormData) => void;
  onCancel: () => void;
  submitLabel?: string;
}

export const ScenarioForm: React.FC<ScenarioFormProps> = ({
  formData,
  isSubmitting,
  error,
  onSubmit,
  onChange,
  onCancel,
  submitLabel = 'Create Scenario'
}) => {
  const mlTypes = [
    'classification',
    'regression',
    'recommendation',
    'image-classification',
    'text-classification',
    'forecasting',
    'object-detection',
  ];

  const handleInputChange = useCallback((field: keyof ScenarioFormData) => (e: any) => {
    let newValue = '';
    if (e.detail && e.detail.value !== undefined) {
      newValue = e.detail.value;
    } else if (e.target && e.target.value !== undefined) {
      newValue = e.target.value;
    }

    onChange({
      ...formData,
      [field]: newValue
    });
  }, [formData, onChange]);

  const handleTagsChange = useCallback((value: string[] | ((prev: string[]) => string[])) => {
    onChange({
      ...formData,
      tags: typeof value === 'function' ? value(formData.tags) : value
    });
  }, [formData, onChange]);

  return (
    <form 
      onSubmit={onSubmit} 
      className="space-y-6 bg-white p-6 rounded-lg shadow"
      onKeyDown={(e) => {
        if (e.key === 'Enter' && e.target instanceof HTMLInputElement) {
          e.preventDefault();
        }
      }}
    >
      {error && (
        <div className="p-4 bg-red-50 text-red-600 rounded-lg">
          {error}
        </div>
      )}

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Scenario Name
        </label>
        <SlInput
          required
          className="w-full"
          value={formData.name}
          onSlInput={handleInputChange('name')}
          placeholder="Enter scenario name..."
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          ML Type
        </label>
        <SlSelect
          required
          className="w-full"
          value={formData.mlType}
          onSlChange={handleInputChange('mlType')}
          placeholder="Select ML Type..."
        >
          {mlTypes.map(type => (
            <SlOption key={type} value={type}>
              {type.replace('-', ' ').replace(/\b\w/g, char => char.toUpperCase())}
            </SlOption>
          ))}
        </SlSelect>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Description
        </label>
        <SlTextarea
          required
          className="w-full"
          value={formData.description}
          onSlInput={handleInputChange('description')}
          placeholder="Enter a detailed description..."
          rows={4}
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Tags
        </label>
        <TagInput
          tags={formData.tags}
          setTags={handleTagsChange}
          placeholder="Type tag and press Enter..."
        />
      </div>

      <div className="flex justify-end gap-4">
        <SlButton
          type="button"
          variant="default"
          onClick={onCancel}
        >
          Cancel
        </SlButton>
        <SlButton
          type="submit"
          variant="primary"
          loading={isSubmitting}
        >
          {submitLabel}
        </SlButton>
      </div>
    </form>
  );
};