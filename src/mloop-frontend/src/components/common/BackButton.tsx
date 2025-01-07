import { useNavigate } from "react-router-dom";
import { SlIcon } from "@shoelace-style/shoelace/dist/react";

interface BackButtonProps {
  scenarioId: string;
}

export const BackButton: React.FC<BackButtonProps> = ({ scenarioId }) => {
  const navigate = useNavigate();

  return (
    <button
      onClick={() => navigate(`/scenarios/${scenarioId}/models`)}
      className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
    >
      <SlIcon name="arrow-left" className="mr-2" />
      Back to Models
    </button>
  );
};
