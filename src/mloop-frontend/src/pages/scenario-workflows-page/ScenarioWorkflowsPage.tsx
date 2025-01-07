import { useState } from "react";
import { useOutletContext } from "react-router-dom";
import { SlAlert, SlTabPanel } from "@shoelace-style/shoelace/dist/react";
import { Scenario } from "../../types/Scenario";
import { WorkflowsHeader } from "./components/WorkflowsHeader";
import { ConfigEditor } from "./components/ConfigEditor";
import { TabPanel } from "./components/TabPanel";
import { useWorkflowConfig } from "./hooks/useWorkflowConfig";
import { useTemplates } from "./hooks/useTemplates";

type ScenarioContextType = {
  scenario: Scenario;
};

export const ScenarioWorkflowsPage = () => {
  const { scenario } = useOutletContext<ScenarioContextType>();
  const [activeTab, setActiveTab] = useState("train");
  const { trainTemplates, predictTemplates, loadTemplate } = useTemplates();

  const {
    config: trainConfig,
    setConfig: setTrainConfig,
    loading: trainLoading,
    error: trainError,
    handleSave: handleTrainSave,
  } = useWorkflowConfig(scenario.scenarioId, "train");

  const {
    config: predictConfig,
    setConfig: setPredictConfig,
    loading: predictLoading,
    error: predictError,
    handleSave: handlePredictSave,
  } = useWorkflowConfig(scenario.scenarioId, "predict");

  const handleTemplateChange = (type: "train" | "predict") => (e: any) => {
    const template = e.detail.value || e.target.value;
    if (template) {
      loadTemplate(type, template).then((content) => {
        if (content) {
          if (type === "train") {
            setTrainConfig(content);
          } else {
            setPredictConfig(content);
          }
        }
      });
    }
  };

  return (
    <div className="p-6">
      <WorkflowsHeader
        title="Workflows"
        description="Configure your machine learning workflows for training and prediction."
      />

      {(trainError || predictError) && (
        <SlAlert variant="danger" className="mb-4">
          {trainError || predictError}
        </SlAlert>
      )}

      <TabPanel activeTab={activeTab} onTabChange={setActiveTab}>
        <SlTabPanel name="train">
          <ConfigEditor
            label="Training Configuration"
            value={trainConfig}
            onChange={(value) => value && setTrainConfig(value)}
            onSave={handleTrainSave}
            onReset={() => setTrainConfig("")}
            templates={trainTemplates}
            onTemplateChange={handleTemplateChange("train")}
            isLoading={trainLoading}
          />
        </SlTabPanel>

        <SlTabPanel name="predict">
          <ConfigEditor
            label="Prediction Configuration"
            value={predictConfig}
            onChange={(value) => value && setPredictConfig(value)}
            onSave={handlePredictSave}
            onReset={() => setPredictConfig("")}
            templates={predictTemplates}
            onTemplateChange={handleTemplateChange("predict")}
            isLoading={predictLoading}
          />
        </SlTabPanel>
      </TabPanel>
    </div>
  );
};
