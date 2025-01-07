import { useState, useCallback, useEffect } from "react";
import { useNotification } from "../../../hooks/useNotification";
import { scenarioApi } from "../../../api/scenarios";

export const useWorkflowConfig = (
  scenarioId: string,
  type: "train" | "predict"
) => {
  const { showNotification } = useNotification();
  const [config, setConfig] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadConfig = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await scenarioApi.getWorkflow(scenarioId, type);
      if (data) {
        setConfig(data);
      }
    } catch (err) {
      console.error("Error loading workflow:", err);
      setError(err instanceof Error ? err.message : "Failed to load workflow");
    } finally {
      setLoading(false);
    }
  }, [scenarioId, type]);

  useEffect(() => {
    loadConfig();
  }, [loadConfig]);

  const handleSave = async () => {
    setLoading(true);
    setError(null);

    try {
      // Validate YAML
      try {
        const jsyaml = await import("js-yaml");
        jsyaml.load(config);
      } catch (yamlError) {
        throw new Error(
          `Invalid YAML format: ${
            yamlError instanceof Error ? yamlError.message : "Unknown error"
          }`
        );
      }

      await scenarioApi.saveWorkflow(scenarioId, type, config);
      showNotification(
        "success",
        `${
          type.charAt(0).toUpperCase() + type.slice(1)
        } workflow updated successfully.`
      );
    } catch (err) {
      const errorMessage =
        err instanceof Error
          ? err.message
          : `Failed to update ${type} workflow.`;
      setError(errorMessage);
      showNotification("danger", errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return {
    config,
    setConfig,
    loading,
    error,
    handleSave,
    loadConfig,
  };
};
