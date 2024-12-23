import { useOutletContext } from 'react-router-dom';
import { Scenario } from '../types/scenarios';

type ScenarioContextType = {
  scenario: Scenario;
};

export const ScenarioWorkflowsPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  
  return (
    <div className="p-6">
      <h2 className="text-2xl font-semibold mb-4">Workflows</h2>
      <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
        <p>No workflows configured for {scenario.name}.</p>
        <p className="mt-2">Create a workflow to automate your ML pipeline.</p>
        <button className="mt-4 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
          Create Workflow
        </button>
      </div>
    </div>
  );
};