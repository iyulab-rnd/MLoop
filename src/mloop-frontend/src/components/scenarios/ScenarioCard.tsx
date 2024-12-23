import { SlCard } from "@shoelace-style/shoelace/dist/react";
import { Scenario } from "../../types/scenarios";

interface ScenarioCardProps {
  scenario: Scenario;
  onClick?: (scenario: Scenario) => void;
}

export const ScenarioCard = ({ scenario, onClick }: ScenarioCardProps) => {
  return (
    <SlCard
      onClick={() => onClick?.(scenario)}
      className="cursor-pointer transition-all duration-300 hover:shadow-lg hover:-translate-y-1 bg-white border border-gray-200 flex-grow h-full"
    >
      <div className="flex flex-col h-full p-4">
        <div className="flex-grow">
          <h3 className="text-xl font-semibold mb-2 text-gray-800 line-clamp-1">
            {scenario.name}
          </h3>
          <p className="text-sm text-gray-600 mb-2 line-clamp-2">
            A machine learning scenario of type {scenario.mlType}
          </p>
          <div className="flex flex-wrap gap-2 mb-2 max-h-[80px] overflow-y-auto">
            {scenario.tags.map((tag: string) => (
              <span
                key={tag}
                className="px-3 py-1 text-xs font-medium rounded-full bg-blue-50 text-blue-600 border border-blue-100"
              >
                {tag}
              </span>
            ))}
          </div>
        </div>
        <div className="text-xs text-gray-500 flex items-center gap-2 mt-auto">
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="2"
              d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
            />
          </svg>
          {new Date(scenario.createdAt).toLocaleDateString("en-US", {
            year: "numeric",
            month: "long",
            day: "numeric",
          })}
        </div>
      </div>
    </SlCard>
  );
};
