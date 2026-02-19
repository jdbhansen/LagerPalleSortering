import type {
  WarehouseDashboardResponse,
  WarehouseOperationResponse,
  WarehousePalletContentsResponse,
  WarehousePalletRecord,
} from '../models';

export interface WarehouseApiClientContract {
  fetchWarehouseDashboard(): Promise<WarehouseDashboardResponse>;
  fetchWarehousePallet(palletId: string): Promise<WarehousePalletRecord>;
  fetchWarehousePalletContents(palletId: string): Promise<WarehousePalletContentsResponse>;
  registerWarehouseColli(
    productNumber: string,
    expiryDateRaw: string,
    quantity: number,
  ): Promise<WarehouseOperationResponse>;
  confirmWarehouseMove(
    scannedPalletCode: string,
    confirmScanCount: number,
  ): Promise<WarehouseOperationResponse>;
  closeWarehousePallet(palletId: string): Promise<WarehouseOperationResponse>;
  undoWarehouseLastEntry(): Promise<WarehouseOperationResponse>;
  clearWarehouseDatabase(): Promise<WarehouseOperationResponse>;
  restoreWarehouseDatabase(file: File): Promise<WarehouseOperationResponse>;
}
