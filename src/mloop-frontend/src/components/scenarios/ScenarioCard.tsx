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
      <div className="flex flex-col h-full p-6">
        {/* ML Type Badge - Moved to top for emphasis */}
        <div className="mb-3">
          <span className="px-3 py-1 text-sm font-medium rounded-md bg-indigo-50 text-indigo-700 border border-indigo-100">
            {scenario.mlType}
          </span>
        </div>

        <div className="flex-grow">
          {/* Title */}
          <h3 className="text-lg font-semibold mb-3 text-gray-900 line-clamp-2">
            {scenario.name}
          </h3>

          {/* Description */}
          <p className="text-sm text-gray-600 mb-4 line-clamp-2">
            {scenario.description || `A machine learning scenario focused on ${scenario.mlType}`}
          </p>

          {/* Tags */}
          <div className="flex flex-wrap gap-2 mb-4 max-h-[80px] overflow-y-auto">
            {scenario.tags.map((tag: string) => (
              <span
                key={tag}
                className="px-2.5 py-1 text-xs font-medium rounded-full bg-gray-50 text-gray-600 border border-gray-100"
              >
                {tag}
              </span>
            ))}
          </div>
        </div>

        {/* Created Date - Bottom */}
        <div className="text-xs text-gray-500 flex items-center gap-2 mt-auto pt-2 border-t border-gray-100">
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