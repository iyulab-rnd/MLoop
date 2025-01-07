import { useNavigate } from 'react-router-dom';
import { SlIcon } from '@shoelace-style/shoelace/dist/react';

export const NavigationHeader = () => {
  const navigate = useNavigate();

  return (
    <button 
      onClick={() => navigate('/scenarios')}
      className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
    >
      <SlIcon name="arrow-left" className="mr-2" />
      Back to Scenarios
    </button>
  );
};
