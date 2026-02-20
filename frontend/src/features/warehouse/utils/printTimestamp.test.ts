import { describe, expect, it } from 'vitest';
import { formatPrintTimestamp } from './printTimestamp';

describe('printTimestamp', () => {
  it('formatterer dansk dato/tid til udskrift', () => {
    const value = formatPrintTimestamp(new Date('2026-02-20T14:05:00'));
    expect(value).toMatch(/^20\.0?2\.2026,\s14\.05\.00$/);
  });
});
