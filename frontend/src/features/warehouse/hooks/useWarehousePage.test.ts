import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { WarehouseApiClientContract } from '../api/warehouseApiClientContract';
import type { WarehouseDashboardResponse, WarehouseOperationResponse } from '../models';
import { useWarehousePage } from './useWarehousePage';

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

describe('useWarehousePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.history.replaceState({}, '', '/app');
  });

  it('afviser register når produkt-input er tomt', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useWarehousePage(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    await act(async () => {
      await result.current.submitRegisterColli();
    });

    expect(result.current.status?.type).toBe('error');
    expect(result.current.status?.message).toContain('Scan kolli stregkode først');
    expect(apiClient.registerWarehouseColli).not.toHaveBeenCalled();
  });

  it('afviser register ved ugyldig holdbarhed', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useWarehousePage(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.updateRegisterFormField('productNumber', 'ITEM-100');
      result.current.updateRegisterFormField('expiryDateRaw', '2026-12-31');
    });

    await act(async () => {
      await result.current.submitRegisterColli();
    });

    expect(result.current.status?.type).toBe('error');
    expect(result.current.status?.message).toContain('Holdbarhed skal være 8 cifre');
    expect(apiClient.registerWarehouseColli).not.toHaveBeenCalled();
  });

  it('normaliserer register-input før API-kald', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useWarehousePage(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.updateRegisterFormField('productNumber', '  item-100  ');
      result.current.setRegisterExpiryRaw('261231');
    });

    await act(async () => {
      await result.current.submitRegisterColli();
    });

    expect(apiClient.registerWarehouseColli).toHaveBeenCalledWith('item-100', '20261231', 1);
  });

  it('udtrækker GS1 produkt + dato ved scan', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useWarehousePage(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.setRegisterProductFromScan('(01)05701234567892(17)261231');
    });

    expect(result.current.registerForm.productNumber).toBe('05701234567892');
    expect(result.current.registerForm.expiryDateRaw).toBe('20261231');
  });

  it('afviser confirm når pallekode mangler og ingen fallback findes', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useWarehousePage(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    await act(async () => {
      await result.current.submitConfirmMove();
    });

    expect(result.current.status?.type).toBe('error');
    expect(result.current.status?.message).toContain('Scan pallelabel');
    expect(apiClient.confirmWarehouseMove).not.toHaveBeenCalled();
  });

  it('bruger fallback palle-id ved tom confirm kode', async () => {
    const apiClient = createApiClientMock();
    const registerResponse: WarehouseOperationResponse = {
      type: 'success',
      message: 'ok',
      palletId: 'P-222',
      createdNewPallet: false,
    };
    vi.mocked(apiClient.registerWarehouseColli).mockResolvedValue(registerResponse);

    const { result } = renderHook(() => useWarehousePage(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => {
      result.current.updateRegisterFormField('productNumber', 'ITEM-222');
      result.current.updateRegisterFormField('expiryDateRaw', '20261231');
    });

    await act(async () => {
      await result.current.submitRegisterColli();
    });

    act(() => {
      result.current.updateConfirmFormField('scannedPalletCode', '   ');
    });

    await act(async () => {
      await result.current.submitConfirmMove();
    });

    expect(apiClient.confirmWarehouseMove).toHaveBeenCalledWith('PALLET:P-222', 1);
  });

  it('restoreDatabase viser fejl hvis fil mangler', async () => {
    const apiClient = createApiClientMock();
    const { result } = renderHook(() => useWarehousePage(apiClient));
    await waitFor(() => expect(result.current.loading).toBe(false));

    await act(async () => {
      await result.current.restoreDatabase();
    });

    expect(result.current.status?.type).toBe('error');
    expect(result.current.status?.message).toContain('Vælg en backupfil først');
    expect(apiClient.restoreWarehouseDatabase).not.toHaveBeenCalled();
  });
});
