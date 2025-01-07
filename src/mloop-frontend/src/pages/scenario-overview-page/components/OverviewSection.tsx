interface OverviewSectionProps {
  title: string;
  content: string;
}

export const OverviewSection: React.FC<OverviewSectionProps> = ({
  title,
  content,
}) => {
  return (
    <div className="mb-6">
      <h2 className="text-lg text-gray-500 mb-2">{title}</h2>
      <p className="text-gray-900 font-medium whitespace-pre-wrap">{content}</p>
    </div>
  );
};
