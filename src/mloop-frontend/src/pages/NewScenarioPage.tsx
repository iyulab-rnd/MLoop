import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ScenarioForm, ScenarioFormData } from '../components/scenarios/ScenarioForm';
import { scenarioApi } from '../api/scenarios';
import { useNotification } from "../hooks/useNotification";

export const NewScenarioPage = () => {
  const navigate = useNavigate();
  const { showNotification } = useNotification();
  const [formData, setFormData] = useState<ScenarioFormData>({
    name: '',
    mlType: '',
    description: '',
    tags: []
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate required fields
    const errors = [];
    if (!formData.name.trim()) {
      errors.push('Name is required');
    }
    if (!formData.mlType.trim()) {
      errors.push('ML Type is required');
    }
    if (formData.tags.length === 0) {
      errors.push('At least one tag is required');
    }

    if (errors.length > 0) {
      setError(errors.join(', '));
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

      showNotification('success', 'Scenario created successfully');
      navigate(`/scenarios/${newScenario.scenarioId}`);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to create scenario';
      setError(errorMessage);
      showNotification('danger', errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="max-w-[800px] mx-auto px-8 py-12">
      <div className="mb-8 flex items-center">
        <button 
          type="button"
          onClick={() => navigate('/scenarios')}
          className="mr-4 text-gray-600 hover:text-gray-900"
        >
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
        </button>
        <h1 className="text-3xl font-bold text-gray-900">Create New Scenario</h1>
      </div>

      <ScenarioForm
        formData={formData}
        isSubmitting={isSubmitting}
        error={error}
        onSubmit={handleSubmit}
        onChange={setFormData}
        onCancel={() => navigate('/scenarios')}
        submitLabel="Create Scenario"
      />
    </div>
  );
};