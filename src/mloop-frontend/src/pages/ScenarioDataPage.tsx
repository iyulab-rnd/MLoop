import { useOutletContext } from 'react-router-dom';
import { Scenario } from '../types/scenarios';

type ScenarioContextType = {
  scenario: Scenario;
};

export const ScenarioDataPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  
  return (
    <div className="p-6">
      <h2 className="text-2xl font-semibold mb-4">Dataset List</h2>
      <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
        <p>No datasets available for {scenario.name} yet.</p>
        <p className="mt-2">Click the button below to add your first dataset.</p>
        <button className="mt-4 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
          Add Dataset
        </button>
      </div>
    </div>
  );
};