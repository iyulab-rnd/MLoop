interface EmptyPredictionsStateProps {
    scenarioName: string;
  }
  
  export const EmptyPredictionsState: React.FC<EmptyPredictionsStateProps> = ({ 
    scenarioName 
  }) => {
    return (
      <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
        <p>No predictions have been made for {scenarioName} yet.</p>
        <p className="mt-2">Start a new prediction to see results here.</p>
      </div>
    );
  };