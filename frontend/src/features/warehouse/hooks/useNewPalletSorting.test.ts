import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { warehouseStorageKeys } from '../constants';
import type { WarehouseApiClientContract } from '../api/warehouseApiClientContract';
import type { WarehouseDashboardResponse, WarehouseOperationResponse } from '../models';
import { useNewPalletSorting } from './useNewPalletSorting';

const navigateToMock = vi.fn();

vi.mock('../../../navigation', () => ({
  navigateTo: (...args: unknown[]) => navigateToMock(...args),
}));

const emptyDashboard: WarehouseDashboardResponse = {
  openPallets: [],
  entries: [],
};

function createApiClientMock(): WarehouseApiClientContract {
  const success: WarehouseOperationResponse = { type: 'success', message: 'ok', palletId: 'P-001' };

  return {
    fetchWarehouseDashboard: vi.fn().mockResolvedValue(emptyDashboard),
    fetchWarehousePallet: vi.fn(),
    fetchWarehousePalletContents: vi.fn(),
    registerWarehouseColli: vi.fn().mockResolvedValue(success),
    confirmWarehouseMove: vi.fn().mockResolvedValue(success),
    closeWarehousePallet: vi.fn().mockResolvedValue(success),
    undoWarehouseLastEntry: vi.fn().mockResolvedValue(success),
    clearWarehouseDatabase: vi.fn().mockResolvedValue(success),
    restoreWarehouseDatabase: vi.fn().mockResolvedValue(success),
  };
}

describe('useNewPalletSorting', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    window.sessionStorage.clear();
    window.history.replaceState({}, '', '/app');
  });

  it('viser fejl og kalder ikke API når produkt-input er tomt', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    await act(async () => {
      await result.current.registerOneColli();
    });

    expect(result.current.status?.type).toBe('error');
    expect(result.current.status?.message).toContain('Scan kolli stregkode først');
    expect(apiClient.registerWarehouseColli).not.toHaveBeenCalled();
  });

  it('viser fejl og kalder ikke API ved ugyldig holdbarhed', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.setProductNumber('ITEM-100');
      result.current.setExpiryDateRaw('2026-12-31');
    });

    await act(async () => {
      await result.current.registerOneColli();
    });

    expect(result.current.status?.type).toBe('error');
    expect(result.current.status?.message).toContain('Holdbarhed skal være 8 cifre');
    expect(apiClient.registerWarehouseColli).not.toHaveBeenCalled();
  });

  it('normaliserer input før register-kald', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.setProductNumber('  item-100  ');
      result.current.setExpiryDateRaw('261231');
    });

    await act(async () => {
      await result.current.registerOneColli();
    });

    expect(apiClient.registerWarehouseColli).toHaveBeenCalledWith('item-100', '20261231', 1);
  });

  it('udtrækker GS1 produkt + holdbarhed fra scan input', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.setProductNumber('(01)05701234567892(17)261231');
    });

    expect(result.current.productNumber).toBe('05701234567892');
    expect(result.current.expiryDateRaw).toBe('20261231');
  });

  it('blokerer confirm før trin 1 er gennemført', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    await act(async () => {
      await result.current.confirmMove();
    });

    expect(result.current.status?.type).toBe('warning');
    expect(result.current.status?.message).toContain('Start med trin 1');
    expect(apiClient.confirmWarehouseMove).not.toHaveBeenCalled();
  });

  it('skifter trin register -> confirm -> register i flowet', async () => {
    const apiClient = createApiClientMock();
    const registerResponse: WarehouseOperationResponse = {
      type: 'success',
      message: 'Ny palle oprettet',
      palletId: 'P-444',
      createdNewPallet: false,
    };
    vi.mocked(apiClient.registerWarehouseColli).mockResolvedValue(registerResponse);

    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.activeStep).toBe('register');

    act(() => {
      result.current.setProductNumber('ITEM-444');
      result.current.setExpiryDateRaw('20261231');
    });

    await act(async () => {
      await result.current.registerOneColli();
    });
    expect(result.current.activeStep).toBe('confirm');

    await act(async () => {
      await result.current.confirmMove();
    });
    expect(result.current.activeStep).toBe('register');
  });

  it('bruger foreslået palle som fallback ved tomt confirm-input', async () => {
    const apiClient = createApiClientMock();
    const registerResponse: WarehouseOperationResponse = {
      type: 'success',
      message: 'Ny palle oprettet',
      palletId: 'P-321',
      createdNewPallet: false,
    };

    vi.mocked(apiClient.registerWarehouseColli).mockResolvedValue(registerResponse);

    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.setProductNumber('ITEM-321');
      result.current.setExpiryDateRaw('20261231');
    });

    await act(async () => {
      await result.current.registerOneColli();
    });

    act(() => {
      result.current.setScannedPalletCode('   ');
    });

    await act(async () => {
      await result.current.confirmMove();
    });

    expect(apiClient.confirmWarehouseMove).toHaveBeenCalledWith('PALLET:P-321', 1);
  });

  it('afviser forkert pallelabel i trin 2', async () => {
    const apiClient = createApiClientMock();
    const registerResponse: WarehouseOperationResponse = {
      type: 'success',
      message: 'Ny palle oprettet',
      palletId: 'P-321',
      createdNewPallet: false,
    };

    vi.mocked(apiClient.registerWarehouseColli).mockResolvedValue(registerResponse);

    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.setProductNumber('ITEM-321');
      result.current.setExpiryDateRaw('20261231');
    });

    await act(async () => {
      await result.current.registerOneColli();
    });

    act(() => {
      result.current.setScannedPalletCode('PALLET:P-999');
    });

    await act(async () => {
      await result.current.confirmMove();
    });

    expect(result.current.status?.type).toBe('error');
    expect(result.current.status?.message).toContain('Forkert pallelabel scannet');
    expect(apiClient.confirmWarehouseMove).not.toHaveBeenCalled();
  });

  it('gemmer started-state i localStorage ved start/stop', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.startNewSorting();
    });

    await waitFor(() => {
      expect(window.localStorage.getItem(warehouseStorageKeys.newSortingActive)).toBe('1');
    });

    act(() => {
      result.current.finishSorting();
    });

    await waitFor(() => {
      expect(window.localStorage.getItem(warehouseStorageKeys.newSortingActive)).toBe('0');
    });
  });

  it('genoptager trin 2 efter print med gemt palle-id', async () => {
    const apiClient = createApiClientMock();
    window.sessionStorage.setItem(warehouseStorageKeys.newSortingPendingPallet, 'P-777');

    const { result } = renderHook(() => useNewPalletSorting(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.activeStep).toBe('confirm');
    expect(result.current.suggestedPalletId).toBe('P-777');
  });
});
