import type { WarehouseDashboardResponse } from '../models';

export interface RegisterFormState {
  productNumber: string;
  expiryDateRaw: string;
  quantity: number;
}

export interface ConfirmFormState {
  scannedPalletCode: string;
  confirmScanCount: number;
}

export type RegisterFormField = keyof RegisterFormState;
export type ConfirmFormField = keyof ConfirmFormState;

export const emptyDashboard: WarehouseDashboardResponse = {
  openPallets: [],
  entries: [],
};

export const defaultRegisterForm: RegisterFormState = {
  productNumber: '',
  expiryDateRaw: '',
  quantity: 1,
};

export const defaultConfirmForm: ConfirmFormState = {
  scannedPalletCode: '',
  confirmScanCount: 1,
};
