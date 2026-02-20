import { describe, expect, it } from 'vitest';
import { normalizeExpiryInput } from './expiryNormalization';

describe('normalizeExpiryInput', () => {
  it('keeps valid YYYYMMDD unchanged', () => {
    expect(normalizeExpiryInput('20261231')).toBe('20261231');
  });

  it('converts YYMMDD to YYYYMMDD', () => {
    expect(normalizeExpiryInput('261231')).toBe('20261231');
  });

  it('keeps invalid dates unchanged', () => {
    expect(normalizeExpiryInput('20260231')).toBe('20260231');
    expect(normalizeExpiryInput('269931')).toBe('269931');
  });
});
