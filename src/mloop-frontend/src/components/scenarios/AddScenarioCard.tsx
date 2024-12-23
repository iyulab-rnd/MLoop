import { SlCard } from '@shoelace-style/shoelace/dist/react';
import { useNavigate } from 'react-router-dom';

interface AddScenarioCardProps {
  onClick?: () => void;
}

export const AddScenarioCard = ({ onClick }: AddScenarioCardProps) => {
  const navigate = useNavigate();

  const handleClick = () => {
    if (onClick) {
      onClick();
    } else {
      navigate('/scenarios/new');
    }
  };

  return (
    <SlCard
      onClick={handleClick}
      className="cursor-pointer transition-all duration-300 hover:shadow-lg hover:-translate-y-1 bg-white h-full flex-grow"
      style={{
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      <div className="text-center text-gray-500">
        <svg
          className="w-12 h-12 mx-auto mb-4"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
        </svg>
        <span className="text-lg font-medium">Add New Scenario</span>
      </div>
    </SlCard>
  );
};