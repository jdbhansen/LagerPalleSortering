import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { RefObject } from 'react';
import {
  warehouseApiClient,
} from '../api/warehouseApiClient';
import type { WarehouseApiClientContract } from '../api/warehouseApiClientContract';
import type { WarehouseDashboardResponse, WarehouseOperationResponse } from '../models';
import { navigateTo } from '../../../navigation';
import { toErrorMessage } from '../../../shared/errorMessage';
import { warehouseBarcodeFormats, warehouseDefaults, warehouseStorageKeys } from '../constants';
import { getPrintLabelPath, getPrintPalletContentsPath } from '../warehouseRouting';

const emptyDashboard: WarehouseDashboardResponse = {
  openPallets: [],
  entries: [],
};

interface NewPalletSortingFormState {
  productNumber: string;
  expiryDateRaw: string;
  scannedPalletCode: string;
  suggestedPalletId: string;
}

const defaultFormState: NewPalletSortingFormState = {
  productNumber: '',
  expiryDateRaw: '',
  scannedPalletCode: '',
  suggestedPalletId: '',
};

export interface NewPalletSortingViewModel {
  loading: boolean;
  started: boolean;
  submitting: boolean;
  dashboard: WarehouseDashboardResponse;
  status: WarehouseOperationResponse | null;
  productNumber: string;
  expiryDateRaw: string;
  scannedPalletCode: string;
  suggestedPalletId: string;
  openColli: number;
  pendingConfirmations: number;
  recentClosedPalletIds: string[];
  productInputRef: RefObject<HTMLInputElement | null>;
  palletInputRef: RefObject<HTMLInputElement | null>;
  reportClientError: (error: unknown) => void;
  setProductNumber: (value: string) => void;
  setExpiryDateRaw: (value: string) => void;
  setScannedPalletCode: (value: string) => void;
  startNewSorting: () => void;
  finishSorting: () => void;
  registerOneColli: () => Promise<void>;
  confirmMove: () => Promise<void>;
  closeCurrentPalletAndPrintLabel: () => Promise<void>;
  closePalletFromList: (palletId: string) => Promise<void>;
  printPalletContentsLabel: (palletId: string) => void;
}

export function useNewPalletSorting(apiClient: WarehouseApiClientContract = warehouseApiClient): NewPalletSortingViewModel {
  const [started, setStarted] = useState(() => window.localStorage.getItem(warehouseStorageKeys.newSortingActive) === '1');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [dashboard, setDashboard] = useState<WarehouseDashboardResponse>(emptyDashboard);
  const [status, setStatus] = useState<WarehouseOperationResponse | null>(null);
  const [formState, setFormState] = useState<NewPalletSortingFormState>(defaultFormState);
  const { productNumber, expiryDateRaw, scannedPalletCode, suggestedPalletId } = formState;

  const productInputRef = useRef<HTMLInputElement | null>(null);
  const palletInputRef = useRef<HTMLInputElement | null>(null);

  const openColli = useMemo(
    () => dashboard.openPallets.reduce((sum, pallet) => sum + pallet.totalQuantity, 0),
    [dashboard.openPallets],
  );

  const pendingConfirmations = useMemo(
    () => dashboard.entries.reduce((sum, entry) => sum + Math.max(0, entry.quantity - entry.confirmedQuantity), 0),
    [dashboard.entries],
  );

  const recentClosedPalletIds = useMemo(() => {
    const openSet = new Set(dashboard.openPallets.map((pallet) => pallet.palletId));
    return Array.from(new Set(
      dashboard.entries
        .map((entry) => entry.palletId)
        .filter((palletId) => !openSet.has(palletId)),
    ));
  }, [dashboard.entries, dashboard.openPallets]);

  const resetFormState = useCallback(() => {
    setFormState(defaultFormState);
  }, []);

  const updateFormField = useCallback(<TField extends keyof NewPalletSortingFormState>(
    field: TField,
    value: NewPalletSortingFormState[TField],
  ) => {
    setFormState((previous) => ({ ...previous, [field]: value }));
  }, []);

  const reloadDashboard = useCallback(async () => {
    const latestDashboard = await apiClient.fetchWarehouseDashboard();
    setDashboard(latestDashboard);
  }, [apiClient]);

  useEffect(() => {
    async function initialize() {
      try {
        await reloadDashboard();
      } catch (error: unknown) {
        setStatus({ type: 'error', message: toErrorMessage(error) });
      } finally {
        setLoading(false);
      }
    }

    void initialize();
  }, [reloadDashboard]);

  useEffect(() => {
    window.localStorage.setItem(warehouseStorageKeys.newSortingActive, started ? '1' : '0');
  }, [started]);

  const startNewSorting = useCallback(() => {
    if (started) {
      setStatus({ type: 'warning', message: 'Afslut den aktive pallesortering før du starter en ny.' });
      return;
    }

    setStarted(true);
    setStatus(null);
    resetFormState();

    window.setTimeout(() => {
      productInputRef.current?.focus();
      productInputRef.current?.select();
    }, warehouseDefaults.focusDelayMs);
  }, [resetFormState, started]);

  const finishSorting = useCallback(() => {
    if (!started) {
      return;
    }

    setStarted(false);
    setSubmitting(false);
    resetFormState();
    setStatus({ type: 'success', message: 'Pallesortering er afsluttet.' });
  }, [resetFormState, started]);

  const registerOneColli = useCallback(async () => {
    if (submitting) {
      return;
    }

    const product = productNumber.trim();
    const expiry = expiryDateRaw.trim();

    if (product.length === 0) {
      setStatus({ type: 'error', message: 'Scan kolli stregkode først.' });
      productInputRef.current?.focus();
      return;
    }

    if (!warehouseBarcodeFormats.expiryDatePattern.test(expiry)) {
      setStatus({ type: 'error', message: 'Holdbarhed skal være 8 cifre i format YYYYMMDD.' });
      return;
    }

    setSubmitting(true);
    try {
      const result = await apiClient.registerWarehouseColli(product, expiry, warehouseDefaults.registerQuantity);
      setStatus(result);

      if (result.type !== 'success') {
        return;
      }

      updateFormField('suggestedPalletId', result.palletId ?? '');
      if (result.createdNewPallet && result.palletId) {
        navigateTo(getPrintLabelPath(result.palletId));
      }

      updateFormField('scannedPalletCode', '');
      await reloadDashboard();

      window.setTimeout(() => {
        palletInputRef.current?.focus();
        palletInputRef.current?.select();
      }, warehouseDefaults.focusDelayMs);
    } catch (error: unknown) {
      setStatus({ type: 'error', message: toErrorMessage(error) });
    } finally {
      setSubmitting(false);
    }
  }, [apiClient, expiryDateRaw, productNumber, reloadDashboard, submitting, updateFormField]);

  const confirmMove = useCallback(async () => {
    if (submitting) {
      return;
    }

    const fallbackCode = suggestedPalletId ? `PALLET:${suggestedPalletId}` : '';
    const palletCode = scannedPalletCode.trim() || fallbackCode;

    if (palletCode.length === 0) {
      setStatus({ type: 'error', message: 'Scan pallelabel for at sætte kollien på plads.' });
      palletInputRef.current?.focus();
      return;
    }

    setSubmitting(true);
    try {
      const result = await apiClient.confirmWarehouseMove(palletCode, warehouseDefaults.confirmScanCount);
      setStatus(result);

      if (result.type !== 'success') {
        return;
      }

      updateFormField('productNumber', '');
      updateFormField('scannedPalletCode', '');
      await reloadDashboard();

      window.setTimeout(() => {
        productInputRef.current?.focus();
        productInputRef.current?.select();
      }, warehouseDefaults.focusDelayMs);
    } catch (error: unknown) {
      setStatus({ type: 'error', message: toErrorMessage(error) });
    } finally {
      setSubmitting(false);
    }
  }, [apiClient, reloadDashboard, scannedPalletCode, submitting, suggestedPalletId, updateFormField]);

  const closeCurrentPalletAndPrintLabel = useCallback(async () => {
    if (!suggestedPalletId || submitting) {
      return;
    }

    setSubmitting(true);
    try {
      const result = await apiClient.closeWarehousePallet(suggestedPalletId);
      setStatus(result);

      if (result.type !== 'success') {
        return;
      }

      await reloadDashboard();
      navigateTo(getPrintPalletContentsPath(suggestedPalletId));
    } catch (error: unknown) {
      setStatus({ type: 'error', message: toErrorMessage(error) });
    } finally {
      setSubmitting(false);
    }
  }, [apiClient, reloadDashboard, submitting, suggestedPalletId]);

  const closePalletFromList = useCallback(async (palletId: string) => {
    if (!palletId || submitting) {
      return;
    }

    setSubmitting(true);
    try {
      const result = await apiClient.closeWarehousePallet(palletId);
      setStatus(result);
      await reloadDashboard();

      if (result.type === 'success' && suggestedPalletId === palletId) {
        updateFormField('suggestedPalletId', '');
      }

      if (result.type === 'success') {
        navigateTo(getPrintPalletContentsPath(palletId));
      }
    } catch (error: unknown) {
      setStatus({ type: 'error', message: toErrorMessage(error) });
    } finally {
      setSubmitting(false);
    }
  }, [apiClient, reloadDashboard, submitting, suggestedPalletId, updateFormField]);

  const printPalletContentsLabel = useCallback((palletId: string) => {
    if (!palletId) {
      return;
    }

    navigateTo(getPrintPalletContentsPath(palletId));
  }, []);

  const reportClientError = useCallback((error: unknown) => {
    setStatus({ type: 'error', message: toErrorMessage(error) });
  }, []);

  return {
    loading,
    started,
    submitting,
    dashboard,
    status,
    productNumber,
    expiryDateRaw,
    scannedPalletCode,
    suggestedPalletId,
    openColli,
    pendingConfirmations,
    recentClosedPalletIds,
    productInputRef,
    palletInputRef,
    setProductNumber: (value) => updateFormField('productNumber', value),
    setExpiryDateRaw: (value) => updateFormField('expiryDateRaw', value),
    setScannedPalletCode: (value) => updateFormField('scannedPalletCode', value),
    startNewSorting,
    finishSorting,
    registerOneColli,
    confirmMove,
    closeCurrentPalletAndPrintLabel,
    closePalletFromList,
    printPalletContentsLabel,
    reportClientError,
  };
}
