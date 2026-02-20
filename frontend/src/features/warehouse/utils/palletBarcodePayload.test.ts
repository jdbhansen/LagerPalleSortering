import { describe, expect, it } from 'vitest';
import { isValidPalletBarcodePayload, toPalletBarcodePayload } from './palletBarcodePayload';

describe('palletBarcodePayload', () => {
  it('bygger canonical payload fra gyldigt palle-id', () => {
    expect(toPalletBarcodePayload('p-001')).toBe('PALLET:P-001');
    expect(toPalletBarcodePayload('P+123')).toBe('PALLET:P-123');
  });

  it('afviser ugyldigt palle-id', () => {
    expect(() => toPalletBarcodePayload('P-ABC')).toThrow();
    expect(() => toPalletBarcodePayload('PALLET:P-001')).toThrow();
  });

  it('validerer payload-format', () => {
    expect(isValidPalletBarcodePayload('PALLET:P-001')).toBe(true);
    expect(isValidPalletBarcodePayload('pallet:p+001')).toBe(true);
    expect(isValidPalletBarcodePayload('PALLET:P-ABC')).toBe(false);
    expect(isValidPalletBarcodePayload('ITEM:123')).toBe(false);
  });
});
