import { SlButton, SlIcon, SlTag } from '@shoelace-style/shoelace/dist/react';
import { Scenario } from '../../../types/Scenario';
import { Model } from '../../../types/Model';

interface ScenarioHeaderProps {
  scenario: Scenario;
  bestModel: Model | null;
  onEdit: () => void;
  onPredict: (modelId: string) => void;
}

export const ScenarioHeader: React.FC<ScenarioHeaderProps> = ({
  scenario,
  bestModel,
  onEdit,
  onPredict,
}) => {
  return (
    <div className="flex justify-between items-start mb-4">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">{scenario.name}</h1>
        <div className="text-sm text-gray-500 mb-4">
          <span className="inline-flex items-center">
            <SlIcon name="calendar" className="mr-2" />
            Created on {new Date(scenario.createdAt).toLocaleDateString('en-US', {
              year: 'numeric',
              month: 'long',
              day: 'numeric'
            })}
          </span>
        </div>
        <div className="mb-3">
          <span className="px-3 py-1 text-sm font-medium rounded-md bg-indigo-50 text-indigo-700 border border-indigo-100">
            {scenario.mlType}
          </span>
        </div>
        <div className="flex flex-wrap gap-2">
          {scenario.tags.map(tag => (
            <SlTag key={tag} variant="neutral">{tag}</SlTag>
          ))}
        </div>
      </div>
      <div className="flex gap-2">
        <SlButton variant="primary" onClick={onEdit}>
          Edit Scenario
        </SlButton>
        {bestModel && (
          <SlButton variant="success" onClick={() => onPredict(bestModel.modelId)}>
            <SlIcon slot="prefix" name="play-fill" />
            Predict
          </SlButton>
        )}
      </div>
    </div>
  );
};
