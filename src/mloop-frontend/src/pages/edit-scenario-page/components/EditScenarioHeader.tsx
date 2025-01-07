import { SlButton, SlIcon } from '@shoelace-style/shoelace/dist/react';

interface EditScenarioHeaderProps {
  onBack: () => void;
  onDelete: () => void;
}

export const EditScenarioHeader: React.FC<EditScenarioHeaderProps> = ({
  onBack,
  onDelete
}) => {
  return (
    <div className="mb-8 flex items-center justify-between">
      <div className="flex items-center">
        <button 
          type="button"
          onClick={onBack}
          className="mr-4 text-gray-600 hover:text-gray-900"
        >
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
        </button>
        <h1 className="text-3xl font-bold text-gray-900">Edit Scenario</h1>
      </div>
      <SlButton variant="danger" onClick={onDelete}>
        <SlIcon slot="prefix" name="trash" />
        Delete Scenario
      </SlButton>
    </div>
  );
};