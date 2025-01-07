import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useNotification } from "../../../hooks/useNotification";
import { scenarioApi } from "../../../api/scenarios";
import { ScenarioFormData } from "../types";

export const useScenarioForm = () => {
  const navigate = useNavigate();
  const { showNotification } = useNotification();
  const [formData, setFormData] = useState<ScenarioFormData>({
    name: "",
    mlType: "",
    description: "",
    tags: [],
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const validateForm = (): string[] => {
    const errors = [];
    if (!formData.name.trim()) {
      errors.push("Name is required");
    }
    if (!formData.mlType.trim()) {
      errors.push("ML Type is required");
    }
    if (formData.tags.length === 0) {
      errors.push("At least one tag is required");
    }
    return errors;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const errors = validateForm();
    if (errors.length > 0) {
      setError(errors.join(", "));
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const newScenario = await scenarioApi.create({
        name: formData.name.trim(),
        mlType: formData.mlType.trim(),
        description: formData.description,
        tags: formData.tags,
      });

      showNotification("success", "Scenario created successfully");
      navigate(`/scenarios/${newScenario.scenarioId}`);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to create scenario";
      setError(errorMessage);
      showNotification("danger", errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    formData,
    setFormData,
    isSubmitting,
    error,
    handleSubmit,
  };
};
