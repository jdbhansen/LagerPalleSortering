import { describe, expect, it } from 'vitest';
import { resolvePalletCode, validateRegisterPayload, validateSuggestedPalletMatch } from './newSortingWorkflow';

describe('newSortingWorkflow', () => {
  it('validerer og normaliserer register payload', () => {
    const result = validateRegisterPayload(' item-1 ', '261231');
    expect(result.success).toBe(true);
    expect(result.value).toEqual({
      product: 'item-1',
      expiry: '20261231',
    });
  });

  it('afviser tomt produktnummer', () => {
    const result = validateRegisterPayload('   ', '20261231');
    expect(result.success).toBe(false);
    expect(result.errorMessage).toContain('Scan kolli stregkode først');
  });

  it('resolver pallekode med fallback', () => {
    expect(resolvePalletCode('   ', 'P-010')).toBe('PALLET:P-010');
    expect(resolvePalletCode('PALLET:P-999', 'P-010')).toBe('PALLET:P-999');
  });

  it('accepterer matchende scannet palle og foreslået palle', () => {
    const result = validateSuggestedPalletMatch('PALLET:P-123', 'P-123');
    expect(result.success).toBe(true);
  });

  it('afviser forkert scannet palle når foreslået palle findes', () => {
    const result = validateSuggestedPalletMatch('PALLET:P-999', 'P-123');
    expect(result.success).toBe(false);
    expect(result.errorMessage).toContain('Forkert pallelabel scannet');
  });
});
