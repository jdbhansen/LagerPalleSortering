import { describe, expect, it } from 'vitest';
import { countOpenColli, countPendingConfirmations } from './warehouseDashboardMetrics';

describe('warehouseDashboardMetrics', () => {
  it('countOpenColli summerer totalQuantity for alle åbne paller', () => {
    const result = countOpenColli({
      openPallets: [
        {
          palletId: 'P-001',
          groupKey: 'A|20260101',
          productNumber: 'A',
          expiryDate: '20260101',
          totalQuantity: 2,
          isClosed: false,
          createdAt: '2026-01-01T00:00:00Z',
        },
        {
          palletId: 'P-002',
          groupKey: 'B|20260101',
          productNumber: 'B',
          expiryDate: '20260101',
          totalQuantity: 5,
          isClosed: false,
          createdAt: '2026-01-01T00:00:00Z',
        },
      ],
      entries: [],
    });

    expect(result).toBe(7);
  });

  it('countPendingConfirmations summerer kun ikke-bekræftet antal', () => {
    const result = countPendingConfirmations({
      openPallets: [],
      entries: [
        {
          id: 1,
          timestamp: '2026-01-01T00:00:00Z',
          palletId: 'P-001',
          groupKey: 'A|20260101',
          productNumber: 'A',
          expiryDate: '20260101',
          quantity: 3,
          createdNewPallet: false,
          confirmedMoved: false,
          confirmedQuantity: 1,
        },
        {
          id: 2,
          timestamp: '2026-01-01T00:00:01Z',
          palletId: 'P-001',
          groupKey: 'B|20260101',
          productNumber: 'B',
          expiryDate: '20260101',
          quantity: 2,
          createdNewPallet: false,
          confirmedMoved: true,
          confirmedAt: '2026-01-01T00:01:00Z',
          confirmedQuantity: 2,
        },
      ],
    });

    expect(result).toBe(2);
  });
});
