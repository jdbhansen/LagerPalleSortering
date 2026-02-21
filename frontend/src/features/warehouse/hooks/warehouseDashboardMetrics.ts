import type { WarehouseDashboardResponse } from '../models';

export function countOpenColli(dashboard: WarehouseDashboardResponse): number {
  return dashboard.openPallets.reduce((sum, pallet) => sum + pallet.totalQuantity, 0);
}

export function countPendingConfirmations(dashboard: WarehouseDashboardResponse): number {
  return dashboard.entries.reduce((sum, entry) => sum + Math.max(0, entry.quantity - entry.confirmedQuantity), 0);
}
