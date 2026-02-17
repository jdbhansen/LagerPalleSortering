import { useEffect, useMemo, useState } from 'react';
import {
  warehouseApiClient,
} from '../api/warehouseApiClient';
import type { WarehouseApiClientContract } from '../api/warehouseApiClientContract';
import type { WarehouseDashboardResponse, WarehouseOperationResponse } from '../models';

interface RegisterFormState {
  productNumber: string;
  expiryDateRaw: string;
  quantity: number;
}

interface ConfirmFormState {
  scannedPalletCode: string;
  confirmScanCount: number;
}

const emptyDashboard: WarehouseDashboardResponse = {
  openPallets: [],
  entries: [],
};

const defaultRegisterForm: RegisterFormState = {
  productNumber: '',
  expiryDateRaw: '',
  quantity: 1,
};

const defaultConfirmForm: ConfirmFormState = {
  scannedPalletCode: '',
  confirmScanCount: 1,
};

function asErrorMessage(error: unknown): string {
  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return 'Ukendt fejl';
}

export function useWarehousePage(apiClient: WarehouseApiClientContract = warehouseApiClient) {
  const [dashboard, setDashboard] = useState<WarehouseDashboardResponse>(emptyDashboard);
  const [loading, setLoading] = useState(true);
  const [isSimpleMode, setIsSimpleMode] = useState(false);
  const [status, setStatus] = useState<WarehouseOperationResponse | null>(null);
  const [showClearWarning, setShowClearWarning] = useState(false);
  const [lastSuggestedPalletId, setLastSuggestedPalletId] = useState('');
  const [registerForm, setRegisterForm] = useState<RegisterFormState>(defaultRegisterForm);
  const [confirmForm, setConfirmForm] = useState<ConfirmFormState>(defaultConfirmForm);
  const [restoreFile, setRestoreFile] = useState<File | null>(null);

  // Derived counters keep rendering components dumb and avoid repeated table traversals.
  const openColli = useMemo(
    () => dashboard.openPallets.reduce((sum, pallet) => sum + pallet.totalQuantity, 0),
    [dashboard.openPallets],
  );

  const pendingConfirmations = useMemo(
    () => dashboard.entries.reduce((sum, entry) => sum + Math.max(0, entry.quantity - entry.confirmedQuantity), 0),
    [dashboard.entries],
  );

  async function reloadDashboard() {
    const latestDashboard = await apiClient.fetchWarehouseDashboard();
    setDashboard(latestDashboard);
  }

  useEffect(() => {
    let isActive = true;

    async function initializeDashboard() {
      try {
        const latestDashboard = await apiClient.fetchWarehouseDashboard();
        if (isActive) {
          setDashboard(latestDashboard);
        }
      } catch (error: unknown) {
        if (isActive) {
          setStatus({ type: 'error', message: asErrorMessage(error) });
        }
      } finally {
        if (isActive) {
          setLoading(false);
        }
      }
    }

    void initializeDashboard();

    return () => {
      // Prevent state updates when async init resolves after unmount.
      isActive = false;
    };
  }, [apiClient]);

  function reportClientError(error: unknown) {
    setStatus({ type: 'error', message: asErrorMessage(error) });
  }

  async function submitRegisterColli() {
    const result = await apiClient.registerWarehouseColli(
      registerForm.productNumber,
      registerForm.expiryDateRaw,
      registerForm.quantity,
    );

    setStatus(result);

    if (result.type !== 'success') {
      return;
    }

    setLastSuggestedPalletId(result.palletId ?? '');
    setRegisterForm((previous) => ({ ...previous, productNumber: '', quantity: 1 }));
    await reloadDashboard();
  }

  async function submitConfirmMove() {
    // UX fallback: if operator does not scan pallet code, reuse latest suggested pallet.
    const fallbackCode = lastSuggestedPalletId ? `PALLET:${lastSuggestedPalletId}` : '';
    const palletCode = confirmForm.scannedPalletCode.trim() || fallbackCode;
    const result = await apiClient.confirmWarehouseMove(palletCode, confirmForm.confirmScanCount);

    setStatus(result);

    if (result.type === 'success') {
      setConfirmForm((previous) => ({ ...previous, scannedPalletCode: '' }));
      await reloadDashboard();
      return;
    }

    if (result.type === 'warning') {
      await reloadDashboard();
    }
  }

  async function closePallet(palletId: string, printContents: boolean) {
    const result = await apiClient.closeWarehousePallet(palletId);
    setStatus(result);
    await reloadDashboard();

    if (printContents) {
      window.open(`/print-pallet-contents/${encodeURIComponent(palletId)}`, '_blank');
    }
  }

  async function undoLastEntry() {
    const result = await apiClient.undoWarehouseLastEntry();
    setStatus(result);
    await reloadDashboard();
  }

  function openClearDatabaseWarning() {
    setShowClearWarning(true);
  }

  function cancelClearDatabaseWarning() {
    setShowClearWarning(false);
    setStatus({ type: 'success', message: 'Ryd database annulleret.' });
  }

  async function clearDatabase() {
    const result = await apiClient.clearWarehouseDatabase();
    setStatus(result);
    setShowClearWarning(false);
    setLastSuggestedPalletId('');
    setRegisterForm(defaultRegisterForm);
    setConfirmForm(defaultConfirmForm);
    await reloadDashboard();
  }

  async function restoreDatabase() {
    if (!restoreFile) {
      setStatus({ type: 'error', message: 'Vælg en backupfil først.' });
      return;
    }

    const result = await apiClient.restoreWarehouseDatabase(restoreFile);
    setStatus(result);
    setRestoreFile(null);
    await reloadDashboard();
  }

  return {
    loading,
    dashboard,
    isSimpleMode,
    status,
    showClearWarning,
    lastSuggestedPalletId,
    openColli,
    pendingConfirmations,
    registerForm,
    confirmForm,
    restoreFile,
    setIsSimpleMode,
    setRegisterForm,
    setConfirmForm,
    setRestoreFile,
    reportClientError,
    submitRegisterColli,
    submitConfirmMove,
    closePallet,
    undoLastEntry,
    openClearDatabaseWarning,
    cancelClearDatabaseWarning,
    clearDatabase,
    restoreDatabase,
  };
}
