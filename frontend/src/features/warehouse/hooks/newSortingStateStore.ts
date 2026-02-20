import { warehouseStorageKeys } from '../constants';

export interface NewSortingStateStore {
  getStarted(): boolean;
  setStarted(started: boolean): void;
  getPendingPalletId(): string;
  setPendingPalletId(palletId: string): void;
  clearPendingPalletId(): void;
}

export class BrowserNewSortingStateStore implements NewSortingStateStore {
  getStarted(): boolean {
    return window.localStorage.getItem(warehouseStorageKeys.newSortingActive) === '1';
  }

  setStarted(started: boolean): void {
    window.localStorage.setItem(warehouseStorageKeys.newSortingActive, started ? '1' : '0');
  }

  getPendingPalletId(): string {
    return window.sessionStorage.getItem(warehouseStorageKeys.newSortingPendingPallet) ?? '';
  }

  setPendingPalletId(palletId: string): void {
    if (!palletId) {
      this.clearPendingPalletId();
      return;
    }

    window.sessionStorage.setItem(warehouseStorageKeys.newSortingPendingPallet, palletId);
  }

  clearPendingPalletId(): void {
    window.sessionStorage.removeItem(warehouseStorageKeys.newSortingPendingPallet);
  }
}

export const browserNewSortingStateStore = new BrowserNewSortingStateStore();
