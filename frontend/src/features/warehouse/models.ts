export interface WarehousePalletRecord {
  palletId: string;
  groupKey: string;
  productNumber: string;
  expiryDate: string;
  totalQuantity: number;
  isClosed: boolean;
  createdAt: string;
}

export interface WarehouseScanEntryRecord {
  id: number;
  timestamp: string;
  productNumber: string;
  expiryDate: string;
  quantity: number;
  palletId: string;
  groupKey: string;
  createdNewPallet: boolean;
  confirmedQuantity: number;
  confirmedMoved: boolean;
  confirmedAt?: string;
}

export interface WarehouseDashboardResponse {
  openPallets: WarehousePalletRecord[];
  entries: WarehouseScanEntryRecord[];
}

export interface WarehousePalletContentsResponse {
  items: WarehousePalletContentItemRecord[];
}

export interface WarehousePalletContentItemRecord {
  productNumber: string;
  expiryDate: string;
  quantity: number;
}

export interface WarehouseOperationResponse {
  type: 'success' | 'warning' | 'error';
  message: string;
  palletId?: string;
  confirmed?: number;
  requested?: number;
  createdNewPallet?: boolean;
}
