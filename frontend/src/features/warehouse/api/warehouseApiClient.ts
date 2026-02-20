import type {
  WarehouseDashboardResponse,
  WarehouseOperationResponse,
  WarehousePalletContentsResponse,
  WarehousePalletRecord,
} from '../models';
import type { WarehouseApiClientContract } from './warehouseApiClientContract';
import { FetchWarehouseHttpClient, type WarehouseApiRoutes, type WarehouseHttpClient } from './warehouseApiInfrastructure';
import { createWarehouseApiRoutes } from './warehouseApiRoutes';

export interface WarehouseApiClientDependencies {
  routes?: WarehouseApiRoutes;
  httpClient?: WarehouseHttpClient;
}

export function createWarehouseApiClient(
  dependencies: WarehouseApiClientDependencies = {},
): WarehouseApiClientContract {
  const routes = dependencies.routes ?? createWarehouseApiRoutes();
  const httpClient = dependencies.httpClient ?? new FetchWarehouseHttpClient();

  return {
    fetchWarehouseDashboard: () =>
      httpClient.requestJson<WarehouseDashboardResponse>(routes.dashboard),

    fetchWarehousePallet: (palletId: string) =>
      httpClient.requestJson<WarehousePalletRecord>(routes.pallet(palletId)),

    fetchWarehousePalletContents: (palletId: string) =>
      httpClient.requestJson<WarehousePalletContentsResponse>(routes.palletContents(palletId)),

    registerWarehouseColli: (productNumber: string, expiryDateRaw: string, quantity: number) =>
      httpClient.requestJson<WarehouseOperationResponse>(routes.register, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productNumber, expiryDateRaw, quantity }),
      }),

    confirmWarehouseMove: (scannedPalletCode: string, confirmScanCount: number) =>
      httpClient.requestJson<WarehouseOperationResponse>(routes.confirm, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ scannedPalletCode, confirmScanCount }),
      }),

    closeWarehousePallet: (palletId: string) =>
      httpClient.requestJson<WarehouseOperationResponse>(routes.closePallet(palletId), {
        method: 'POST',
      }),

    undoWarehouseLastEntry: () =>
      httpClient.requestJson<WarehouseOperationResponse>(routes.undo, {
        method: 'POST',
      }),

    clearWarehouseDatabase: () =>
      httpClient.requestJson<WarehouseOperationResponse>(routes.clear, {
        method: 'POST',
      }),

    restoreWarehouseDatabase: (file: File) => {
      const formData = new FormData();
      formData.set('file', file);

      return httpClient.requestJson<WarehouseOperationResponse>(routes.restore, {
        method: 'POST',
        body: formData,
      });
    },
  };
}

export const warehouseApiClient: WarehouseApiClientContract = createWarehouseApiClient();

export const fetchWarehouseDashboard = (): Promise<WarehouseDashboardResponse> =>
  warehouseApiClient.fetchWarehouseDashboard();

export const fetchWarehousePallet = (palletId: string): Promise<WarehousePalletRecord> =>
  warehouseApiClient.fetchWarehousePallet(palletId);

export const fetchWarehousePalletContents = (palletId: string): Promise<WarehousePalletContentsResponse> =>
  warehouseApiClient.fetchWarehousePalletContents(palletId);

export const registerWarehouseColli = (
  productNumber: string,
  expiryDateRaw: string,
  quantity: number,
): Promise<WarehouseOperationResponse> =>
  warehouseApiClient.registerWarehouseColli(productNumber, expiryDateRaw, quantity);

export const confirmWarehouseMove = (
  scannedPalletCode: string,
  confirmScanCount: number,
): Promise<WarehouseOperationResponse> =>
  warehouseApiClient.confirmWarehouseMove(scannedPalletCode, confirmScanCount);

export const closeWarehousePallet = (palletId: string): Promise<WarehouseOperationResponse> =>
  warehouseApiClient.closeWarehousePallet(palletId);

export const undoWarehouseLastEntry = (): Promise<WarehouseOperationResponse> =>
  warehouseApiClient.undoWarehouseLastEntry();

export const clearWarehouseDatabase = (): Promise<WarehouseOperationResponse> =>
  warehouseApiClient.clearWarehouseDatabase();

export const restoreWarehouseDatabase = (file: File): Promise<WarehouseOperationResponse> =>
  warehouseApiClient.restoreWarehouseDatabase(file);
