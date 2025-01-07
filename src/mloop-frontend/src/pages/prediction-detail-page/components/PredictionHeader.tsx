import { useNavigate } from "react-router-dom";
import { SlButton, SlIcon } from "@shoelace-style/shoelace/dist/react";

interface PredictionHeaderProps {
  scenarioId: string;
  onRefresh: () => void;
}

export const PredictionHeader: React.FC<PredictionHeaderProps> = ({
  scenarioId,
  onRefresh,
}) => {
  const navigate = useNavigate();

  return (
    <div className="mb-6">
      <button
        onClick={() => navigate(`/scenarios/${scenarioId}/predictions`)}
        className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
      >
        <SlIcon name="arrow-left" className="mr-2" />
        Back to Predictions
      </button>

      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-semibold">Files</h2>
          <p className="text-gray-600 mt-1">
            View and download prediction files
          </p>
        </div>
        <div className="flex gap-2">
          <SlButton variant="primary" onClick={onRefresh}>
            <SlIcon slot="prefix" name="arrow-clockwise" />
            Refresh
          </SlButton>
        </div>
      </div>
    </div>
  );
};
