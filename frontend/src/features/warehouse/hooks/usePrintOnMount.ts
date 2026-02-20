import { useEffect } from 'react';
import { warehouseDefaults } from '../constants';

export function usePrintOnMount(enabled = true) {
  useEffect(() => {
    if (!enabled) {
      return;
    }

    const timer = window.setTimeout(() => {
      window.print();
    }, warehouseDefaults.printDelayMs);

    return () => {
      window.clearTimeout(timer);
    };
  }, [enabled]);
}
