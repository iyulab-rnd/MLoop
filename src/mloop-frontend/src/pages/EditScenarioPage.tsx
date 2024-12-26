import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ScenarioForm, ScenarioFormData } from '../components/scenarios/ScenarioForm';
import { scenarioApi } from '../api/scenarios';
import { useNotification } from '../contexts/NotificationContext';

export const EditScenarioPage = () => {
  const navigate = useNavigate();
  const { scenarioId } = useParams();
  const { showNotification } = useNotification();
  const [formData, setFormData] = useState<ScenarioFormData>({
    name: '',
    mlType: '',
    description: '',
    tags: []
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchScenario = async () => {
      try {
        setLoading(true);
        if (!scenarioId) {
          throw new Error('Scenario ID is required');
        }
        const data = await scenarioApi.get(scenarioId);
        setFormData({
          name: data.name,
          mlType: data.mlType,
          description: data.description,
          tags: data.tags
        });
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to load scenario';
        setError(errorMessage);
        showNotification('danger', errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchScenario();
  }, [scenarioId, showNotification]);

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

    if (!scenarioId) {
      showNotification('danger', 'Scenario ID is required');
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      await scenarioApi.update(scenarioId, {
        name: formData.name.trim(),
        mlType: formData.mlType.trim(),
        description: formData.description,
        tags: formData.tags,
      });

      showNotification('success', 'Scenario updated successfully');
      navigate(`/scenarios/${scenarioId}`);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update scenario';
      setError(errorMessage);
      showNotification('danger', errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="max-w-[800px] mx-auto px-8 py-12">
      <div className="mb-8 flex items-center">
        <button 
          type="button"
          onClick={() => navigate(`/scenarios/${scenarioId}`)}
          className="mr-4 text-gray-600 hover:text-gray-900"
        >
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
        </button>
        <h1 className="text-3xl font-bold text-gray-900">Edit Scenario</h1>
      </div>

      <ScenarioForm
        formData={formData}
        isSubmitting={isSubmitting}
        error={error}
        onSubmit={handleSubmit}
        onChange={setFormData}
        onCancel={() => navigate(`/scenarios/${scenarioId}`)}
        submitLabel="Save Changes"
      />
    </div>
  );
};