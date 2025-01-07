import { useState, useCallback } from "react";
import { useNotification } from "../useNotification";

export function useActionStatus() {
  const { showNotification } = useNotification();
  const [isLoading, setIsLoading] = useState(false);

  const execute = useCallback(
    async <T>(
      action: () => Promise<T>,
      successMessage?: string
    ): Promise<T | undefined> => {
      try {
        setIsLoading(true);
        const result = await action();
        if (successMessage) {
          showNotification("success", successMessage);
        }
        return result;
      } catch (err) {
        const message =
          err instanceof Error ? err.message : "An error occurred";
        showNotification("danger", message);
      } finally {
        setIsLoading(false);
      }
    },
    [showNotification]
  );

  return {
    isLoading,
    execute,
  };
}
