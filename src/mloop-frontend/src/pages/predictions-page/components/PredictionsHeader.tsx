import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';

interface PredictionsHeaderProps {
  hasData: boolean;
  onCleanup: () => void;
  cleanupInProgress: boolean;
}

export const PredictionsHeader: React.FC<PredictionsHeaderProps> = ({
  hasData,
  onCleanup,
  cleanupInProgress
}) => {
  return (
    <div className="flex justify-between items-center mb-6">
      <div>
        <h2 className="text-2xl font-semibold">Predictions</h2>
        <p className="text-gray-600 mt-1">
          View and manage predictions for this scenario
        </p>
      </div>
      <div className="flex gap-2">
        {hasData && (
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