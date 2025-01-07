import { useNavigate } from 'react-router-dom';
import { SlIcon } from '@shoelace-style/shoelace/dist/react';
import { Model } from '../../../types/Model';

interface ModelDetailHeaderProps {
  model: Model;
  scenarioId: string;
}

export const ModelDetailHeader: React.FC<ModelDetailHeaderProps> = ({
  model,
  scenarioId,
}) => {
  const navigate = useNavigate();

  return (
    <div className="mb-6">
      <button
        onClick={() => navigate(`/scenarios/${scenarioId}/models`)}
        className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
      >
        <SlIcon name="arrow-left" className="mr-2" />
        Back to Models
      </button>

      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">
            Model {model.modelId}
          </h1>
          <div className="flex items-center gap-4">
            <span className="px-3 py-1 text-sm font-medium rounded-md bg-indigo-50 text-indigo-700 border border-indigo-100">
              {model.mlType}
            </span>
            <span className="text-sm text-gray-500">
              Created on {new Date(model.createdAt).toLocaleString()}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};