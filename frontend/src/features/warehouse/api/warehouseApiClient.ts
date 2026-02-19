import type {
  WarehouseDashboardResponse,
  WarehouseOperationResponse,
  WarehousePalletContentsResponse,
  WarehousePalletRecord,
} from '../models';
import type { WarehouseApiClientContract } from './warehouseApiClientContract';

const warehouseApiRoutes = {
  dashboard: '/api/warehouse/dashboard',
  register: '/api/warehouse/register',
  confirm: '/api/warehouse/confirm',
  undo: '/api/warehouse/undo',
  clear: '/api/warehouse/clear',
  restore: '/api/warehouse/restore',
  pallet: (palletId: string) => `/api/warehouse/pallets/${encodeURIComponent(palletId)}`,
  palletContents: (palletId: string) => `/api/warehouse/pallets/${encodeURIComponent(palletId)}/contents`,
  closePallet: (palletId: string) => `/api/warehouse/pallets/${encodeURIComponent(palletId)}/close`,
} as const;

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
  return requestJson<WarehouseDashboardResponse>(warehouseApiRoutes.dashboard);
}

export function fetchWarehousePallet(palletId: string): Promise<WarehousePalletRecord> {
  return requestJson<WarehousePalletRecord>(warehouseApiRoutes.pallet(palletId));
}

export function fetchWarehousePalletContents(palletId: string): Promise<WarehousePalletContentsResponse> {
  return requestJson<WarehousePalletContentsResponse>(warehouseApiRoutes.palletContents(palletId));
}

export function registerWarehouseColli(productNumber: string, expiryDateRaw: string, quantity: number): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>(warehouseApiRoutes.register, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ productNumber, expiryDateRaw, quantity }),
  });
}

export function confirmWarehouseMove(scannedPalletCode: string, confirmScanCount: number): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>(warehouseApiRoutes.confirm, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ scannedPalletCode, confirmScanCount }),
  });
}

export function closeWarehousePallet(palletId: string): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>(warehouseApiRoutes.closePallet(palletId), {
    method: 'POST',
  });
}

export function undoWarehouseLastEntry(): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>(warehouseApiRoutes.undo, {
    method: 'POST',
  });
}

export function clearWarehouseDatabase(): Promise<WarehouseOperationResponse> {
  return requestJson<WarehouseOperationResponse>(warehouseApiRoutes.clear, {
    method: 'POST',
  });
}

export function restoreWarehouseDatabase(file: File): Promise<WarehouseOperationResponse> {
  const formData = new FormData();
  formData.set('file', file);

  return requestJson<WarehouseOperationResponse>(warehouseApiRoutes.restore, {
    method: 'POST',
    body: formData,
  });
}

export const warehouseApiClient: WarehouseApiClientContract = {
  fetchWarehouseDashboard,
  fetchWarehousePallet,
  fetchWarehousePalletContents,
  registerWarehouseColli,
  confirmWarehouseMove,
  closeWarehousePallet,
  undoWarehouseLastEntry,
  clearWarehouseDatabase,
  restoreWarehouseDatabase,
};
