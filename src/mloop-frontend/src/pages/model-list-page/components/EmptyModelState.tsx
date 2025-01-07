interface EmptyModelStateProps {
    scenarioName: string;
  }
  
  export const EmptyModelState: React.FC<EmptyModelStateProps> = ({ 
    scenarioName 
  }) => {
    return (
      <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
        <p>No models have been trained for {scenarioName} yet.</p>
        <p className="mt-2">Start a training job to create new models.</p>
      </div>
    );
  };
  