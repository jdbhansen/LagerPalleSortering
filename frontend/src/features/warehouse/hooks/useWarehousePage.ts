import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  warehouseApiClient,
} from '../api/warehouseApiClient';
import type { WarehouseApiClientContract } from '../api/warehouseApiClientContract';
import type { WarehouseDashboardResponse, WarehouseOperationResponse } from '../models';
import { navigateTo } from '../../../navigation';
import { toErrorMessage } from '../../../shared/errorMessage';
import { getPrintLabelPath, getPrintPalletContentsPath } from '../warehouseRouting';
import { normalizeExpiryInput } from '../utils/expiryNormalization';
import { parseGs1ProductAndExpiry } from '../utils/gs1Parser';
import { resolvePalletCode, validateRegisterPayload, validateSuggestedPalletMatch } from './newSortingWorkflow';

interface RegisterFormState {
  productNumber: string;
  expiryDateRaw: string;
  quantity: number;
}

interface ConfirmFormState {
  scannedPalletCode: string;
  confirmScanCount: number;
}

type RegisterFormField = keyof RegisterFormState;
type ConfirmFormField = keyof ConfirmFormState;

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

  const reloadDashboard = useCallback(async () => {
    const latestDashboard = await apiClient.fetchWarehouseDashboard();
    setDashboard(latestDashboard);
  }, [apiClient]);

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
          setStatus({ type: 'error', message: toErrorMessage(error) });
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

  const reportClientError = useCallback((error: unknown) => {
    setStatus({ type: 'error', message: toErrorMessage(error) });
  }, []);

  function updateRegisterFormField<TField extends RegisterFormField>(
    field: TField,
    value: RegisterFormState[TField],
  ) {
    setRegisterForm((previous) => ({ ...previous, [field]: value }));
  }

  function updateConfirmFormField<TField extends ConfirmFormField>(
    field: TField,
    value: ConfirmFormState[TField],
  ) {
    setConfirmForm((previous) => ({ ...previous, [field]: value }));
  }

  async function submitRegisterColli() {
    const rawManualExpiry = registerForm.expiryDateRaw.trim();
    const payloadResult = validateRegisterPayload(registerForm.productNumber, registerForm.expiryDateRaw);
    const manualExpiry = normalizeExpiryInput(registerForm.expiryDateRaw).trim();

    if (!payloadResult.success) {
      setStatus({ type: 'error', message: payloadResult.errorMessage ?? 'Ugyldigt input.' });
      return;
    }

    if (registerForm.quantity <= 0) {
      setStatus({ type: 'error', message: 'Antal kolli skal være mindst 1.' });
      return;
    }

    if (rawManualExpiry.length > 0 && !/^\d{8}$/.test(manualExpiry)) {
      setStatus({ type: 'error', message: 'Holdbarhed skal være 8 cifre i format YYYYMMDD.' });
      return;
    }

    const payload = payloadResult.value!;
    // Keep request payload normalized centrally to match backend grouping/parsing rules.
    const result = await apiClient.registerWarehouseColli(
      payload.product,
      payload.expiry,
      registerForm.quantity,
    );

    setStatus(result);

    if (result.type !== 'success') {
      return;
    }

    setLastSuggestedPalletId(result.palletId ?? '');
    if (result.createdNewPallet && result.palletId) {
      navigateTo(getPrintLabelPath(result.palletId));
    }
    updateRegisterFormField('productNumber', '');
    updateRegisterFormField('quantity', 1);
    await reloadDashboard();
  }

  async function submitConfirmMove() {
    const palletMatch = validateSuggestedPalletMatch(confirmForm.scannedPalletCode, lastSuggestedPalletId);
    if (!palletMatch.success) {
      setStatus({ type: 'error', message: palletMatch.errorMessage ?? 'Forkert pallelabel scannet.' });
      return;
    }

    // UX fallback: if operator does not scan pallet code, reuse latest suggested pallet.
    const palletCode = resolvePalletCode(confirmForm.scannedPalletCode, lastSuggestedPalletId);
    if (palletCode.length === 0) {
      setStatus({ type: 'error', message: 'Scan pallelabel for at sætte kollien på plads.' });
      return;
    }

    const result = await apiClient.confirmWarehouseMove(palletCode, confirmForm.confirmScanCount);

    setStatus(result);

    if (result.type === 'success') {
      updateConfirmFormField('scannedPalletCode', '');
      await reloadDashboard();
      return;
    }

    if (result.type === 'warning') {
      // Warning may still indicate partial confirmations; refresh keeps dashboard state truthful.
      await reloadDashboard();
    }
  }

  async function closePallet(palletId: string, printContents: boolean) {
    const result = await apiClient.closeWarehousePallet(palletId);
    setStatus(result);
    await reloadDashboard();

    if (printContents) {
      navigateTo(getPrintPalletContentsPath(palletId));
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
    updateRegisterFormField,
    setRegisterProductFromScan: (value: string) => {
      const parsedScan = parseGs1ProductAndExpiry(value);
      if (!parsedScan) {
        updateRegisterFormField('productNumber', value);
        return;
      }

      updateRegisterFormField('productNumber', parsedScan.productNumber ?? value);
      if (parsedScan.expiryDateRaw) {
        updateRegisterFormField('expiryDateRaw', parsedScan.expiryDateRaw);
      }
    },
    updateConfirmFormField,
    setRegisterExpiryRaw: (value: string) => updateRegisterFormField('expiryDateRaw', normalizeExpiryInput(value)),
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
