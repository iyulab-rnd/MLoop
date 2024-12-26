import { useState, useEffect } from 'react';
import { useNotification } from '../../contexts/NotificationContext';
import { Scenario } from '../../types/Scenario';
import { scenarioApi } from '../../api/scenarios';

export const useScenarios = () => {
  const { showNotification } = useNotification();
  const [scenarios, setScenarios] = useState<Scenario[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const loadScenarios = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await scenarioApi.list({ searchTerm });
        setScenarios(data);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to fetch scenarios';
        console.error('Error in useScenarios:', err);
        showNotification('danger', errorMessage);
        setError(err instanceof Error ? err : new Error(errorMessage));
        setScenarios([]);
      } finally {
        setLoading(false);
      }
    };

    loadScenarios();
  }, [searchTerm, showNotification]);

  return {
    scenarios,
    searchTerm,
    setSearchTerm,
    loading,
    error
  };
};