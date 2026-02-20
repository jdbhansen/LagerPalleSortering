import { describe, expect, it } from 'vitest';
import { formatExpiryDateForDisplay } from './expiryDate';

describe('formatExpiryDateForDisplay', () => {
  it('formatterer YYYYMMDD til YYYY-MM-DD', () => {
    expect(formatExpiryDateForDisplay('20261231')).toBe('2026-12-31');
  });

  it('returnerer input uændret ved ugyldigt format', () => {
    expect(formatExpiryDateForDisplay('2026-12-31')).toBe('2026-12-31');
    expect(formatExpiryDateForDisplay('abc')).toBe('abc');
  });
});
