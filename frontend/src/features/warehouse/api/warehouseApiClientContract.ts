import type { WarehouseDashboardResponse, WarehouseOperationResponse } from '../models';

export interface WarehouseApiClientContract {
  fetchWarehouseDashboard(): Promise<WarehouseDashboardResponse>;
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
