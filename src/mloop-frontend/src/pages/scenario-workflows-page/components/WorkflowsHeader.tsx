interface WorkflowsHeaderProps {
  title: string;
  description: string;
}

export const WorkflowsHeader: React.FC<WorkflowsHeaderProps> = ({
  title,
  description,
}) => {
  return (
    <div className="mb-6">
      <h2 className="text-2xl font-semibold mb-4">{title}</h2>
      <p className="text-gray-600">{description}</p>
    </div>
  );
};
