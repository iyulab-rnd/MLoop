import { useState, useCallback } from "react";
import { useNotification } from "../useNotification";

interface UseAsyncDataOptions<T> {
  onSuccess?: (data: T) => void;
  onError?: (error: Error) => void;
  defaultValue?: T;
}

export function useAsyncData<T>(options: UseAsyncDataOptions<T> = {}) {
  const { showNotification } = useNotification();
  const [data, setData] = useState<T | undefined>(options.defaultValue);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const execute = useCallback(
    async (asyncFn: () => Promise<T>) => {
      try {
        setLoading(true);
        setError(null);
        const result = await asyncFn();
        setData(result);
        options.onSuccess?.(result);
        return result;
      } catch (err) {
        const error =
          err instanceof Error ? err : new Error("An error occurred");
        setError(error);
        showNotification("danger", error.message);
        options.onError?.(error);
        throw error;
      } finally {
        setLoading(false);
      }
    },
    [options, showNotification]
  );

  return {
    data,
    setData,
    loading,
    error,
    execute,
  };
}
