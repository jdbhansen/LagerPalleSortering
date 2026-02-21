import { useEffect, useState } from 'react';
import { warehouseStorageKeys } from '../constants';

const defaultAutoPrintEnabled = true;

function readStoredAutoPrintSetting(): boolean {
  const raw = window.localStorage.getItem(warehouseStorageKeys.printAutoMode);
  if (raw === null) {
    return defaultAutoPrintEnabled;
  }

  return raw === '1';
}

function readStoredPrinterName(): string {
  return window.localStorage.getItem(warehouseStorageKeys.preferredPrinterName) ?? '';
}

export function usePrintPreferences() {
  const [autoPrintEnabled, setAutoPrintEnabled] = useState<boolean>(readStoredAutoPrintSetting);
  const [preferredPrinterName, setPreferredPrinterName] = useState<string>(readStoredPrinterName);

  useEffect(() => {
    window.localStorage.setItem(warehouseStorageKeys.printAutoMode, autoPrintEnabled ? '1' : '0');
  }, [autoPrintEnabled]);

  useEffect(() => {
    const trimmedName = preferredPrinterName.trim();
    if (!trimmedName) {
      window.localStorage.removeItem(warehouseStorageKeys.preferredPrinterName);
      return;
    }

    window.localStorage.setItem(warehouseStorageKeys.preferredPrinterName, trimmedName);
  }, [preferredPrinterName]);

  return {
    autoPrintEnabled,
    setAutoPrintEnabled,
    preferredPrinterName,
    setPreferredPrinterName,
  };
}
