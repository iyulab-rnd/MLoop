interface MLTypeSectionProps {
  mlType: string;
}

export const MLTypeSection: React.FC<MLTypeSectionProps> = ({ mlType }) => {
  return (
    <div className="mb-6">
      <h2 className="text-lg text-gray-500 mb-2">ML Type</h2>
      <div className="inline-block px-3 py-1 bg-indigo-50 text-indigo-700 rounded-md border border-indigo-100">
        {mlType}
      </div>
    </div>
  );
};
