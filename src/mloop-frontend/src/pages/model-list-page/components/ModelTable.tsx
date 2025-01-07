import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';
import { useNavigate } from 'react-router-dom';
import { Model } from '../../../types/Model';
import { formatTime } from '../../../utils/time';

interface ModelTableProps {
  models: Model[];
  scenarioId: string;
  onDeleteModel: (modelId: string) => Promise<void>;
}

export const ModelTable: React.FC<ModelTableProps> = ({
  models,
  scenarioId,
  onDeleteModel,
}) => {
  const navigate = useNavigate();

  return (
    <div className="bg-white rounded-lg border border-gray-200">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th
              scope="col"
              className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
            >
              Model ID
            </th>
            <th
              scope="col"
              className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
            >
              ML Type
            </th>
            <th
              scope="col"
              className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
            >
              Best Score
            </th>
            <th
              scope="col"
              className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
            >
              Runtime
            </th>
            <th
              scope="col"
              className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
            >
              Created At
            </th>
            <th scope="col" className="relative px-6 py-3">
              <span className="sr-only">Actions</span>
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {models.map((model) => (
            <tr key={model.modelId} className="hover:bg-gray-50">
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm font-medium text-gray-900">
                  {model.modelId}
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <span className="px-2 py-1 text-xs font-medium rounded-md bg-indigo-50 text-indigo-700">
                  {model.mlType}
                </span>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm text-gray-900">
                  {(model.metrics.BestScore * 100).toFixed(2)}%
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm text-gray-900">
                  {formatTime(model.metrics.BestRuntime)}
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {new Date(model.createdAt).toLocaleString()}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-right">
                <div className="flex justify-end gap-2">
                  <SlButton
                    size="small"
                    variant="primary"
                    onClick={(e) => {
                      e.stopPropagation();
                      navigate(
                        `/scenarios/${scenarioId}/models/${model.modelId}/predict`
                      );
                    }}
                  >
                    <SlIcon slot="prefix" name="play" />
                    Predict
                  </SlButton>
                  <SlButton
                    size="small"
                    variant="default"
                    onClick={(e) => {
                      e.stopPropagation();
                      navigate(
                        `/scenarios/${scenarioId}/models/${model.modelId}`
                      );
                    }}
                  >
                    <SlIcon slot="prefix" name="eye" />
                    View
                  </SlButton>
                  <SlButton
                    size="small"
                    variant="danger"
                    onClick={async (e) => {
                      e.stopPropagation();
                      await onDeleteModel(model.modelId);
                    }}
                  >
                    <SlIcon slot="prefix" name="trash" />
                    Delete
                  </SlButton>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};