import { useState, useEffect } from 'react';
import { Scenario } from '../../types/scenarios';
import { fetchScenarios } from '../../api/scenarios';

export const useScenarios = () => {
  const [scenarios, setScenarios] = useState<Scenario[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const loadScenarios = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await fetchScenarios({ searchTerm });
        setScenarios(data);
      } catch (err) {
        console.error('Error in useScenarios:', err);
        setError(err instanceof Error ? err : new Error('An error occurred while fetching scenarios'));
        setScenarios([]); // 에러 시 scenarios 초기화
      } finally {
        setLoading(false);
      }
    };

    loadScenarios();
  }, [searchTerm]);

  return {
    scenarios,
    searchTerm,
    setSearchTerm,
    loading,
    error
  };
};