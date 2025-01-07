interface EmptyJobStateProps {
    scenarioName: string;
  }
  
  export const EmptyJobState: React.FC<EmptyJobStateProps> = ({ scenarioName }) => {
    return (
      <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
        <p>No jobs have been run for {scenarioName} yet.</p>
        <p className="mt-2">Start a new training job to see results here.</p>
      </div>
    );
  };