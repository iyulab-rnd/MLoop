import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';

interface ModelListHeaderProps {
  modelCount: number;
  onStartTraining: () => void;
  onCleanup: () => void;
  trainingInProgress: boolean;
  cleanupInProgress: boolean;
}

export const ModelListHeader: React.FC<ModelListHeaderProps> = ({
  modelCount,
  onStartTraining,
  onCleanup,
  trainingInProgress,
  cleanupInProgress,
}) => {
  return (
    <div className="flex justify-between items-center mb-6">
      <div>
        <h2 className="text-2xl font-semibold">Trained Models</h2>
        <p className="text-gray-600 mt-1">
          View and manage trained models for this scenario
        </p>
      </div>
      <div className="flex gap-2">
        <SlButton
          variant="primary"
          onClick={onStartTraining}
          loading={trainingInProgress}
        >
          <SlIcon slot="prefix" name="play" />
          Start Training
        </SlButton>
        {modelCount > 0 && (
          <SlButton
            variant="neutral"
            onClick={onCleanup}
            loading={cleanupInProgress}
          >
            <SlIcon slot="prefix" name="trash" />
            Cleanup
          </SlButton>
        )}
      </div>
    </div>
  );
};