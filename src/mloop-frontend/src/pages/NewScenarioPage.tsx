import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ScenarioForm, ScenarioFormData } from '../components/scenarios/ScenarioForm';

export const NewScenarioPage = () => {
  const navigate = useNavigate();
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
    if (!formData.name.trim() || !formData.mlType.trim() || !formData.description.trim()) {
      setError('Name, ML Type, and Description are required');
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const response = await fetch('/api/scenarios', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: formData.name.trim(),
          mlType: formData.mlType.trim(),
          description: formData.description.trim(),
          tags: formData.tags,
        }),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to create scenario');
      }

      navigate('/scenarios');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
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