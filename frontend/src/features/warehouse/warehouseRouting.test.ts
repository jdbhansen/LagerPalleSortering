import { describe, expect, it } from 'vitest';
import {
  getPrintLabelPath,
  getPrintPalletContentsPath,
  getWarehousePrintRoute,
} from './warehouseRouting';

describe('warehouseRouting', () => {
  it('parser print-label route korrekt', () => {
    const route = getWarehousePrintRoute('/app/print-label/P-001', '');
    expect(route).toEqual({ type: 'label', palletId: 'P-001' });
  });

  it('parser print-contents route korrekt', () => {
    const route = getWarehousePrintRoute('/app/print-pallet-contents/P-010', '?format=label190x100');
    expect(route).toEqual({ type: 'contents', palletId: 'P-010', format: 'label190x100' });
  });

  it('returnerer null for ikke-print route', () => {
    expect(getWarehousePrintRoute('/app', '')).toBeNull();
  });

  it('bygger print routes med encoding', () => {
    expect(getPrintLabelPath('P 01/2')).toBe('/app/print-label/P%2001%2F2');
    expect(getPrintPalletContentsPath('P 01/2')).toBe('/app/print-pallet-contents/P%2001%2F2?format=label190x100');
  });
});
