import { useEffect, useRef } from 'react';
import { warehouseDefaults } from '../constants';

interface UseAutoRefreshDashboardInput {
  refresh: () => Promise<void>;
  onError: (error: unknown) => void;
  enabled?: boolean;
}

export function useAutoRefreshDashboard({
  refresh,
  onError,
  enabled = true,
}: UseAutoRefreshDashboardInput) {
  const runningRef = useRef(false);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const runRefresh = async () => {
      if (runningRef.current) {
        return;
      }

      runningRef.current = true;
      try {
        await refresh();
      } catch (error: unknown) {
        onError(error);
      } finally {
        runningRef.current = false;
      }
    };

    const interval = window.setInterval(() => {
      void runRefresh();
    }, warehouseDefaults.dashboardRefreshMs);

    const onVisibilityOrFocus = () => {
      if (document.visibilityState === 'visible') {
        void runRefresh();
      }
    };

    window.addEventListener('focus', onVisibilityOrFocus);
    document.addEventListener('visibilitychange', onVisibilityOrFocus);

    return () => {
      window.clearInterval(interval);
      window.removeEventListener('focus', onVisibilityOrFocus);
      document.removeEventListener('visibilitychange', onVisibilityOrFocus);
    };
  }, [enabled, onError, refresh]);
}
