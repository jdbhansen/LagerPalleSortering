import type { WarehouseDashboardResponse, WarehouseOperationResponse } from '../models';
import type { WarehouseApiClientContract } from './warehouseApiClientContract';

async function requestJson<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
  const response = await fetch(input, init);
  // Some backend failures may return empty/non-JSON bodies, so parsing must be defensive.
  const payload = await response.json().catch(() => null);

  if (!response.ok) {
    const message = payload?.message ?? 'Netværksfejl';
    throw new Error(message);
  }

  return payload as T;
}

export function fetchWarehouseDashboard(): Promise<WarehouseDashboardResponse> {
  return requestJson<WarehouseDashboardResponse>('/api/warehouse/dashboard');
}

export function registerWarehouseColli(productNumber: string, expiryDateRaw: string, quantity: number): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>('/api/warehouse/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ productNumber, expiryDateRaw, quantity }),
  });
}

export function confirmWarehouseMove(scannedPalletCode: string, confirmScanCount: number): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>('/api/warehouse/confirm', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ scannedPalletCode, confirmScanCount }),
  });
}

export function closeWarehousePallet(palletId: string): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>(`/api/warehouse/pallets/${encodeURIComponent(palletId)}/close`, {
    method: 'POST',
  });
}

export function undoWarehouseLastEntry(): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>('/api/warehouse/undo', {
    method: 'POST',
  });
}

export function clearWarehouseDatabase(): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>('/api/warehouse/clear', {
    method: 'POST',
  });
}

export function restoreWarehouseDatabase(file: File): Promise<WarehouseOperationResponse> {
  const formData = new FormData();
  formData.set('file', file);

  return requestJson<WarehouseOperationResponse>('/api/warehouse/restore', {
    method: 'POST',
    body: formData,
  });
}

export const warehouseApiClient: WarehouseApiClientContract = {
  fetchWarehouseDashboard,
  registerWarehouseColli,
  confirmWarehouseMove,
  closeWarehousePallet,
  undoWarehouseLastEntry,
  clearWarehouseDatabase,
  restoreWarehouseDatabase,
};
