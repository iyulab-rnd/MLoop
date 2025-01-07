import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { scenarioApi } from '../../../api/scenarios';
import { useNotification } from '../../../hooks/useNotification';
import { ScenarioFormData } from '../../scenario-list-page/components/ScenarioForm';

export const useEditScenario = (scenarioId: string | undefined) => {
  const navigate = useNavigate();
  const { showNotification } = useNotification();
  const [formData, setFormData] = useState<ScenarioFormData>({
    name: '',
    mlType: '',
    description: '',
    tags: []
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
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

  const handleDelete = async () => {
    if (!scenarioId) return;

    setIsDeleting(true);
    try {
      await scenarioApi.delete(scenarioId);
      showNotification('success', 'Scenario deleted successfully');
      navigate('/scenarios');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete scenario';
      showNotification('danger', errorMessage);
    } finally {
      setIsDeleting(false);
      setShowDeleteDialog(false);
    }
  };

  return {
    formData,
    setFormData,
    isSubmitting,
    isDeleting,
    showDeleteDialog,
    setShowDeleteDialog,
    error,
    loading,
    handleSubmit,
    handleDelete
  };
};