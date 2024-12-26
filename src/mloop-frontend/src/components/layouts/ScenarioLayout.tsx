import { useState, useEffect } from "react";
import {
  useParams,
  useNavigate,
  useLocation,
  Link,
  Outlet,
} from "react-router-dom";
import { SlButton, SlIcon, SlTag } from "@shoelace-style/shoelace/dist/react";
import { Scenario } from "../../types/Scenario";
import { scenarioApi } from "../../api/scenarios";
import { Model } from "../../types";

const TabItem = ({
  to,
  current,
  children,
}: {
  to: string;
  current: boolean;
  children: React.ReactNode;
}) => (
  <Link
    to={to}
    className={`px-4 py-2 font-medium text-sm rounded-md transition-colors
      ${
        current
          ? "bg-white text-blue-600 shadow"
          : "text-gray-600 hover:text-gray-900 hover:bg-white/60"
      }`}
  >
    {children}
  </Link>
);

export const ScenarioLayout = () => {
  const { scenarioId } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const [scenario, setScenario] = useState<Scenario | null>(null);
  const [bestModel, setBestModel] = useState<Model | null>(null); // 베스트 모델 상태 추가
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const scenarioData = await scenarioApi.get(scenarioId!);
        setScenario(scenarioData);

        // 모델 목록을 가져와서 베스트 모델 찾기
        const models = await scenarioApi.listModels(scenarioId!);
        // BestScore 기준으로 정렬하여 최상위 모델 선택
        const sortedModels = models.sort(
          (a, b) => b.metrics.BestScore - a.metrics.BestScore
        );
        setBestModel(sortedModels[0] || null);
      } catch (err) {
        setError(err instanceof Error ? err : new Error("An error occurred"));
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [scenarioId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-[800px] mx-auto px-8 py-12">
        <div className="p-4 bg-red-50 text-red-600 rounded-lg text-center">
          {error.message}
        </div>
      </div>
    );
  }

  if (!scenario) {
    return (
      <div className="max-w-[800px] mx-auto px-8 py-12">
        <div className="p-4 bg-yellow-50 text-yellow-600 rounded-lg text-center">
          Scenario not found
        </div>
      </div>
    );
  }

  const currentPath = location.pathname;
  const baseUrl = `/scenarios/${scenarioId}`;

  return (
    <div className="max-w-[1200px] mx-auto px-8 py-12">
      <div className="mb-8">
        <button
          onClick={() => navigate("/scenarios")}
          className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
        >
          <SlIcon name="arrow-left" className="mr-2" />
          Back to Scenarios
        </button>

        <div className="bg-white rounded-lg shadow">
          {/* Header */}
          <div className="p-6 border-b border-gray-200">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h1 className="text-3xl font-bold text-gray-900 mb-2">
                  {scenario.name}
                </h1>
                <div className="text-sm text-gray-500 mb-4">
                  <span className="inline-flex items-center">
                    <SlIcon name="calendar" className="mr-2" />
                    Created on{" "}
                    {new Date(scenario.createdAt).toLocaleDateString("en-US", {
                      year: "numeric",
                      month: "long",
                      day: "numeric",
                    })}
                  </span>
                </div>
                {/* Added ML Type */}
                <div className="mb-3">
                  <span className="px-3 py-1.5 text-sm font-medium rounded-md bg-indigo-50 text-indigo-700 border border-indigo-100">
                    {scenario.mlType}
                  </span>
                </div>
                <div className="flex flex-wrap gap-2">
                  {scenario.tags.map((tag) => (
                    <SlTag key={tag} variant="neutral">
                      {tag}
                    </SlTag>
                  ))}
                </div>
              </div>

              <div className="flex gap-2">
                <SlButton
                  variant="primary"
                  onClick={() => navigate(`${baseUrl}/edit`)}
                >
                  Edit Scenario
                </SlButton>
                {bestModel && (
                  <SlButton
                    variant="success"
                    onClick={() =>
                      navigate(
                        `/scenarios/${scenarioId}/models/${bestModel.modelId}/predict`
                      )
                    }
                  >
                    <SlIcon slot="prefix" name="play-fill" />
                    Predict
                  </SlButton>
                )}
              </div>
            </div>

            {/* Tab Navigation */}
            <div className="flex gap-2 mt-6 bg-gray-100 p-1 rounded-md">
              <TabItem to={baseUrl} current={currentPath === baseUrl}>
                Overview
              </TabItem>
              <TabItem
                to={`${baseUrl}/models`}
                current={currentPath.includes("/models")}
              >
                Models
              </TabItem>
              <TabItem
                to={`${baseUrl}/data`}
                current={currentPath.includes("/data")}
              >
                Data
              </TabItem>
              <TabItem
                to={`${baseUrl}/workflows`}
                current={currentPath.includes("/workflows")}
              >
                Workflows
              </TabItem>
              <TabItem
                to={`${baseUrl}/predictions`}
                current={currentPath.includes("/predictions")}
              >
                Predictions
              </TabItem>
              <TabItem
                to={`${baseUrl}/jobs`}
                current={currentPath.includes("/jobs")}
              >
                Jobs
              </TabItem>
            </div>
          </div>

          {/* Content Area */}
          <Outlet context={{ scenario }} />
        </div>
      </div>
    </div>
  );
};
