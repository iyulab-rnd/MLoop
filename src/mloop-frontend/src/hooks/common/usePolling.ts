import { useEffect, useRef } from "react";

interface UsePollingOptions {
  interval?: number;
  enabled?: boolean;
}

export function usePolling(
  callback: () => Promise<void>,
  { interval = 3000, enabled = true }: UsePollingOptions = {}
) {
  const timeoutRef = useRef<number>();

  useEffect(() => {
    if (!enabled) return;

    const poll = async () => {
      try {
        await callback();
      } finally {
        timeoutRef.current = window.setTimeout(poll, interval);
      }
    };

    poll();

    return () => {
      if (timeoutRef.current) {
        window.clearTimeout(timeoutRef.current);
      }
    };
  }, [callback, interval, enabled]);
}
