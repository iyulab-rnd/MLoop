interface EmptyDataStateProps {
    onUpload: () => void;
  }
  
  export const EmptyDataState: React.FC<EmptyDataStateProps> = ({ onUpload }) => {
    return (
      <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
        <p>No datasets available in this directory.</p>
        <p className="mt-2">
          <button 
            className="text-blue-600 hover:underline"
            onClick={onUpload}
          >
            Click here
          </button>{" "}
          to upload your files.
        </p>
      </div>
    );
  };