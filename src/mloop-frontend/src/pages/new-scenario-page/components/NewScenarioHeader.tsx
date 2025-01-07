import { useNavigate } from "react-router-dom";
import { SlIcon } from "@shoelace-style/shoelace/dist/react";

export const NewScenarioHeader = () => {
  const navigate = useNavigate();

  return (
    <div className="mb-8 flex items-center">
      <button
        type="button"
        onClick={() => navigate("/scenarios")}
        className="mr-4 text-gray-600 hover:text-gray-900"
      >
        <SlIcon name="arrow-left" className="w-6 h-6" />
      </button>
      <h1 className="text-3xl font-bold text-gray-900">Create New Scenario</h1>
    </div>
  );
};
