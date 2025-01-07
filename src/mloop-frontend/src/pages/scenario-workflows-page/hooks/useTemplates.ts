import { useState, useEffect } from "react";
import { useNotification } from "../../../hooks/useNotification";

export const useTemplates = () => {
  const { showNotification } = useNotification();
  const [trainTemplates, setTrainTemplates] = useState<string[]>([]);
  const [predictTemplates, setPredictTemplates] = useState<string[]>([]);

  useEffect(() => {
    const loadTemplates = async () => {
      try {
        // Load train templates
        const trainResponse = await fetch("/templates/train/list.txt");
        if (!trainResponse.ok) {
          throw new Error("Failed to load training templates list");
        }
        const trainList = await trainResponse.text();
        const trainTemplateNames = trainList
          .split("\n")
          .map((line) => line.trim())
          .filter((line) => line && line.endsWith(".yaml"));
        setTrainTemplates(trainTemplateNames);

        // Load predict templates
        const predictResponse = await fetch("/templates/predict/list.txt");
        if (predictResponse.ok) {
          const predictList = await predictResponse.text();
          const predictTemplateNames = predictList
            .split("\n")
            .map((line) => line.trim())
            .filter((line) => line && line.endsWith(".yaml"));
          setPredictTemplates(predictTemplateNames);
        }
      } catch (err) {
        console.error("Error loading templates:", err);
        showNotification("danger", "Failed to load templates list");
      }
    };

    loadTemplates();
  }, [showNotification]);

  const loadTemplate = async (
    type: "train" | "predict",
    templateName: string
  ) => {
    try {
      const response = await fetch(`/templates/${type}/${templateName}`);
      if (!response.ok) {
        throw new Error(`Failed to load ${type} template`);
      }

      const content = await response.text();
      showNotification("success", "Template loaded successfully");
      return content;
    } catch (err) {
      console.error("Error loading template:", err);
      showNotification("danger", "Failed to load template");
      return null;
    }
  };

  return {
    trainTemplates,
    predictTemplates,
    loadTemplate,
  };
};
