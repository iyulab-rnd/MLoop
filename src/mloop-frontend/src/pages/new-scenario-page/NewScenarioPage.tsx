import { useNavigate } from "react-router-dom";
import { NewScenarioHeader } from "./components/NewScenarioHeader";
import { ScenarioForm } from "./components/ScenarioForm";
import { useScenarioForm } from "./hooks/useScenarioForm";

export const NewScenarioPage = () => {
  const navigate = useNavigate();
  const { formData, setFormData, isSubmitting, error, handleSubmit } =
    useScenarioForm();

  return (
    <div className="max-w-[800px] mx-auto px-8 py-12">
      <NewScenarioHeader />
      <ScenarioForm
        formData={formData}
        isSubmitting={isSubmitting}
        error={error}
        onSubmit={handleSubmit}
        onChange={setFormData}
        onCancel={() => navigate("/scenarios")}
      />
    </div>
  );
};
