import { SlCard } from "@shoelace-style/shoelace/dist/react";
import { Scenario } from "../../../types/Scenario";

interface MetricsSectionProps {
  scenario: Scenario;
}

export const MetricsSection: React.FC<MetricsSectionProps> = ({}) => {
  // Add any metrics calculation logic here
  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
      <SlCard className="p-4">
        <h3 className="text-sm text-gray-500 mb-1">Total Models</h3>
        <p className="text-2xl font-semibold text-gray-900">-</p>
      </SlCard>
      <SlCard className="p-4">
        <h3 className="text-sm text-gray-500 mb-1">Latest Training</h3>
        <p className="text-2xl font-semibold text-gray-900">-</p>
      </SlCard>
      <SlCard className="p-4">
        <h3 className="text-sm text-gray-500 mb-1">Best Score</h3>
        <p className="text-2xl font-semibold text-gray-900">-</p>
      </SlCard>
    </div>
  );
};
