export type WarehouseViewMode = 'newSorting' | 'fullOverview';

export const warehouseStorageKeys = {
  viewMode: 'lagerpallesortering:viewMode',
  newSortingActive: 'lagerpallesortering:new-sorting-active',
  newSortingPendingPallet: 'lagerpallesortering:new-sorting-pending-pallet',
  printAutoMode: 'lagerpallesortering:print-auto-mode',
  preferredPrinterName: 'lagerpallesortering:preferred-printer-name',
} as const;

export const warehouseBarcodeFormats = {
  expiryDatePattern: /^\d{8}$/,
} as const;

export const warehouseDefaults = {
  confirmScanCount: 1,
  registerQuantity: 1,
  printDelayMs: 100,
  focusDelayMs: 0,
  dashboardRefreshMs: 10000,
} as const;
