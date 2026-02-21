import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { RefObject } from 'react';
import {
  warehouseApiClient,
} from '../api/warehouseApiClient';
import type { WarehouseApiClientContract } from '../api/warehouseApiClientContract';
import type { WarehouseDashboardResponse, WarehouseOperationResponse } from '../models';
import { navigateTo } from '../../../navigation';
import { toErrorMessage } from '../../../shared/errorMessage';
import { warehouseDefaults } from '../constants';
import { getPrintLabelPath, getPrintPalletContentsPath } from '../warehouseRouting';
import { normalizeExpiryInput } from '../utils/expiryNormalization';
import { parseGs1ProductAndExpiry } from '../utils/gs1Parser';
import {
  browserNewSortingStateStore,
  type NewSortingStateStore,
} from './newSortingStateStore';
import {
  resolvePalletCode,
  type NewPalletSortingStep,
  validateSuggestedPalletMatch,
  validateRegisterPayload,
} from './newSortingWorkflow';

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
  activeStep: NewPalletSortingStep;
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
  palletContentsRefreshToken: number;
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

export function useNewPalletSorting(
  apiClient: WarehouseApiClientContract = warehouseApiClient,
  stateStore: NewSortingStateStore = browserNewSortingStateStore,
): NewPalletSortingViewModel {
  const initialPendingPalletId = stateStore.getPendingPalletId();
  const [started, setStarted] = useState(() => stateStore.getStarted());
  const [activeStep, setActiveStep] = useState<NewPalletSortingStep>(initialPendingPalletId ? 'confirm' : 'register');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [dashboard, setDashboard] = useState<WarehouseDashboardResponse>(emptyDashboard);
  const [status, setStatus] = useState<WarehouseOperationResponse | null>(null);
  const [palletContentsRefreshToken, setPalletContentsRefreshToken] = useState(0);
  const [formState, setFormState] = useState<NewPalletSortingFormState>(() => ({
    ...defaultFormState,
    suggestedPalletId: initialPendingPalletId,
  }));
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
    stateStore.setStarted(started);
  }, [started, stateStore]);

  useEffect(() => {
    if (!started || loading || submitting) {
      return;
    }

    const timer = window.setTimeout(() => {
      if (activeStep === 'register') {
        productInputRef.current?.focus();
        productInputRef.current?.select();
        return;
      }

      palletInputRef.current?.focus();
      palletInputRef.current?.select();
    }, warehouseDefaults.focusDelayMs);

    return () => {
      window.clearTimeout(timer);
    };
  }, [activeStep, loading, started, submitting]);

  const reportClientError = useCallback((error: unknown) => {
    setStatus({ type: 'error', message: toErrorMessage(error) });
  }, []);

  const startNewSorting = useCallback(() => {
    if (started) {
      setStatus({ type: 'warning', message: 'Afslut den aktive pallesortering før du starter en ny.' });
      return;
    }

    setStarted(true);
    setActiveStep('register');
    setStatus(null);
    resetFormState();
    stateStore.clearPendingPalletId();

    window.setTimeout(() => {
      productInputRef.current?.focus();
      productInputRef.current?.select();
    }, warehouseDefaults.focusDelayMs);
  }, [resetFormState, started, stateStore]);

  const finishSorting = useCallback(() => {
    if (!started) {
      return;
    }

    setStarted(false);
    setActiveStep('register');
    setSubmitting(false);
    resetFormState();
    stateStore.clearPendingPalletId();
    setStatus({ type: 'success', message: 'Pallesortering er afsluttet.' });
  }, [resetFormState, started, stateStore]);

  const registerOneColli = useCallback(async () => {
    if (submitting) {
      return;
    }

    if (activeStep !== 'register') {
      setStatus({ type: 'warning', message: 'Fuldfør først trin 2: scan palle.' });
      return;
    }

    const payloadResult = validateRegisterPayload(productNumber, expiryDateRaw);
    if (!payloadResult.success) {
      setStatus({ type: 'error', message: payloadResult.errorMessage ?? 'Ugyldigt input.' });
      productInputRef.current?.focus();
      return;
    }

    setSubmitting(true);
    try {
      const payload = payloadResult.value!;
      const result = await apiClient.registerWarehouseColli(payload.product, payload.expiry, warehouseDefaults.registerQuantity);
      setStatus(result);

      if (result.type !== 'success') {
        return;
      }

      updateFormField('suggestedPalletId', result.palletId ?? '');
      if (result.palletId) {
        stateStore.setPendingPalletId(result.palletId);
      }
      if (result.createdNewPallet && result.palletId) {
        navigateTo(getPrintLabelPath(result.palletId));
      }

      setActiveStep('confirm');
      updateFormField('scannedPalletCode', '');
      await reloadDashboard();
      setPalletContentsRefreshToken((previous) => previous + 1);

      window.setTimeout(() => {
        palletInputRef.current?.focus();
        palletInputRef.current?.select();
      }, warehouseDefaults.focusDelayMs);
    } catch (error: unknown) {
      setStatus({ type: 'error', message: toErrorMessage(error) });
    } finally {
      setSubmitting(false);
    }
  }, [activeStep, apiClient, expiryDateRaw, productNumber, reloadDashboard, stateStore, submitting, updateFormField]);

  const confirmMove = useCallback(async () => {
    if (submitting) {
      return;
    }

    if (activeStep !== 'confirm') {
      setStatus({ type: 'warning', message: 'Start med trin 1: scan kolli og holdbarhed.' });
      return;
    }

    const palletMatch = validateSuggestedPalletMatch(scannedPalletCode, suggestedPalletId);
    if (!palletMatch.success) {
      setStatus({ type: 'error', message: palletMatch.errorMessage ?? 'Forkert pallelabel scannet.' });
      palletInputRef.current?.focus();
      palletInputRef.current?.select();
      return;
    }

    const palletCode = resolvePalletCode(scannedPalletCode, suggestedPalletId);
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

      setActiveStep('register');
      updateFormField('productNumber', '');
      updateFormField('expiryDateRaw', '');
      updateFormField('scannedPalletCode', '');
      stateStore.clearPendingPalletId();
      await reloadDashboard();
      setPalletContentsRefreshToken((previous) => previous + 1);

      window.setTimeout(() => {
        productInputRef.current?.focus();
        productInputRef.current?.select();
      }, warehouseDefaults.focusDelayMs);
    } catch (error: unknown) {
      setStatus({ type: 'error', message: toErrorMessage(error) });
    } finally {
      setSubmitting(false);
    }
  }, [activeStep, apiClient, reloadDashboard, scannedPalletCode, stateStore, submitting, suggestedPalletId, updateFormField]);

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
      setPalletContentsRefreshToken((previous) => previous + 1);
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
      setPalletContentsRefreshToken((previous) => previous + 1);

      if (result.type === 'success' && suggestedPalletId === palletId) {
        updateFormField('suggestedPalletId', '');
        stateStore.clearPendingPalletId();
      }

      if (result.type === 'success') {
        navigateTo(getPrintPalletContentsPath(palletId));
      }
    } catch (error: unknown) {
      setStatus({ type: 'error', message: toErrorMessage(error) });
    } finally {
      setSubmitting(false);
    }
  }, [apiClient, reloadDashboard, stateStore, submitting, suggestedPalletId, updateFormField]);

  const printPalletContentsLabel = useCallback((palletId: string) => {
    if (!palletId) {
      return;
    }

    navigateTo(getPrintPalletContentsPath(palletId));
  }, []);

  return {
    loading,
    started,
    activeStep,
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
    palletContentsRefreshToken,
    productInputRef,
    palletInputRef,
    setProductNumber: (value) => {
      const parsedScan = parseGs1ProductAndExpiry(value);
      if (!parsedScan) {
        updateFormField('productNumber', value);
        return;
      }

      updateFormField('productNumber', parsedScan.productNumber ?? value);
      if (parsedScan.expiryDateRaw) {
        updateFormField('expiryDateRaw', parsedScan.expiryDateRaw);
      }
    },
    setExpiryDateRaw: (value) => updateFormField('expiryDateRaw', normalizeExpiryInput(value)),
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
