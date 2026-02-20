import { describe, expect, it } from 'vitest';
import { parseGs1ProductAndExpiry } from './gs1Parser';

describe('parseGs1ProductAndExpiry', () => {
  it('parses parenthesized GS1 fields', () => {
    const result = parseGs1ProductAndExpiry('(01)05701234567890(17)261231(10)LOT123');

    expect(result).toEqual({
      productNumber: '05701234567890',
      expiryDateRaw: '20261231',
    });
  });

  it('parses compact GS1 payload', () => {
    const result = parseGs1ProductAndExpiry('01057012345678901726123110LOT123');

    expect(result).toEqual({
      productNumber: '05701234567890',
      expiryDateRaw: '20261231',
    });
  });

  it('parses scanner-prefixed payload with group separator', () => {
    const result = parseGs1ProductAndExpiry(']Q3010570123456789017261231\u001d10LOT123');

    expect(result).toEqual({
      productNumber: '05701234567890',
      expiryDateRaw: '20261231',
    });
  });

  it('returns null for non-gs1 payloads', () => {
    expect(parseGs1ProductAndExpiry('ITEM-ABC')).toBeNull();
  });

  it('returns product even when expiry AI has invalid date', () => {
    const result = parseGs1ProductAndExpiry('(01)05701234567890(17)269931');

    expect(result).toEqual({
      productNumber: '05701234567890',
      expiryDateRaw: null,
    });
  });
});
