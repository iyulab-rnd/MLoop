import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ScenarioForm, ScenarioFormData } from '../components/scenarios/ScenarioForm';
import { Scenario } from '../types/scenarios';

export const EditScenarioPage = () => {
  const navigate = useNavigate();
  const { scenarioId } = useParams();
  const [formData, setFormData] = useState<ScenarioFormData>({
    name: '',
    mlType: '',
    description: '',
    tags: []
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  // 기존 시나리오 데이터 로드
  useEffect(() => {
    const fetchScenario = async () => {
      try {
        const response = await fetch(`/api/scenarios/${scenarioId}`);
        if (!response.ok) {
          throw new Error('Failed to fetch scenario');
        }
        const data: Scenario = await response.json();
        setFormData({
          name: data.name,
          mlType: data.mlType,
          description: data.description,
          tags: data.tags
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load scenario');
      } finally {
        setLoading(false);
      }
    };

    fetchScenario();
  }, [scenarioId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.mlType.trim() || !formData.description.trim()) {
      setError('Name, ML Type, and Description are required');
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const response = await fetch(`/api/scenarios/${scenarioId}`, {
        method: 'PUT',
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
        throw new Error(errorText || 'Failed to update scenario');
      }

      navigate(`/scenarios/${scenarioId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
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