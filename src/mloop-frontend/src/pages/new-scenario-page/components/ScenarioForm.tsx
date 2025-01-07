import {
  SlButton,
  SlInput,
  SlSelect,
  SlTextarea,
} from "@shoelace-style/shoelace/dist/react";
import { TagInput } from "../../../components/common/TagInput";
import { ScenarioFormData } from "../types";

interface ScenarioFormProps {
  formData: ScenarioFormData;
  isSubmitting: boolean;
  error: string | null;
  onSubmit: (e: React.FormEvent) => void;
  onChange: (data: ScenarioFormData) => void;
  onCancel: () => void;
}

export const ScenarioForm: React.FC<ScenarioFormProps> = ({
  formData,
  isSubmitting,
  error,
  onSubmit,
  onChange,
  onCancel,
}) => {
  return (
    <form onSubmit={onSubmit} className="space-y-6">
      {error && (
        <div className="p-4 bg-red-50 text-red-600 rounded-lg text-sm">
          {error}
        </div>
      )}

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Name
        </label>
        <SlInput
          className="w-full"
          value={formData.name}
          onSlInput={(e: any) =>
            onChange({ ...formData, name: e.target.value })
          }
          placeholder="Enter scenario name"
          required
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          ML Type
        </label>
        <SlSelect
          className="w-full"
          value={formData.mlType}
          onSlChange={(e: any) =>
            onChange({ ...formData, mlType: e.target.value })
          }
          required
        >
          <option value="">Select ML type</option>
          <option value="classification">Classification</option>
          <option value="regression">Regression</option>
          <option value="image-classification">Image Classification</option>
        </SlSelect>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Description
        </label>
        <SlTextarea
          className="w-full"
          value={formData.description}
          onSlInput={(e: any) =>
            onChange({ ...formData, description: e.target.value })
          }
          placeholder="Enter scenario description"
          rows={4}
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Tags
        </label>
        <TagInput
          tags={formData.tags}
          setTags={(tags) =>
            onChange({
              ...formData,
              tags: Array.isArray(tags) ? tags : tags(formData.tags),
            })
          }
          placeholder="Add tags..."
        />
      </div>

      <div className="flex justify-end gap-4">
        <SlButton
          type="button"
          variant="default"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancel
        </SlButton>
        <SlButton type="submit" variant="primary" loading={isSubmitting}>
          Create Scenario
        </SlButton>
      </div>
    </form>
  );
};
