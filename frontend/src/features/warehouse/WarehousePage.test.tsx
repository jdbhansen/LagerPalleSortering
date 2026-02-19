import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { WarehouseDashboardResponse, WarehouseOperationResponse } from './models';
import { WarehousePage } from './WarehousePage';

vi.mock('jsbarcode', () => ({
  default: vi.fn(),
}));

const apiMocks = vi.hoisted(() => ({
  fetchWarehouseDashboard: vi.fn(),
  fetchWarehousePalletContents: vi.fn(),
  registerWarehouseColli: vi.fn(),
  confirmWarehouseMove: vi.fn(),
  closeWarehousePallet: vi.fn(),
  undoWarehouseLastEntry: vi.fn(),
  clearWarehouseDatabase: vi.fn(),
  restoreWarehouseDatabase: vi.fn(),
}));

vi.mock('./api/warehouseApiClient', () => ({
  ...apiMocks,
  warehouseApiClient: { ...apiMocks },
}));

import {
  clearWarehouseDatabase,
  closeWarehousePallet,
  confirmWarehouseMove,
  fetchWarehouseDashboard,
  fetchWarehousePalletContents,
  registerWarehouseColli,
  restoreWarehouseDatabase,
  undoWarehouseLastEntry,
} from './api/warehouseApiClient';

const emptyDashboard: WarehouseDashboardResponse = {
  openPallets: [],
  entries: [],
};

const successResponse: WarehouseOperationResponse = {
  type: 'success',
  message: 'ok',
  palletId: 'P-001',
};

describe('WarehousePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchWarehouseDashboard).mockResolvedValue(emptyDashboard);
    vi.mocked(fetchWarehousePalletContents).mockResolvedValue({ items: [] });
    vi.mocked(registerWarehouseColli).mockResolvedValue(successResponse);
    vi.mocked(confirmWarehouseMove).mockResolvedValue(successResponse);
    vi.mocked(closeWarehousePallet).mockResolvedValue(successResponse);
    vi.mocked(undoWarehouseLastEntry).mockResolvedValue(successResponse);
    vi.mocked(clearWarehouseDatabase).mockResolvedValue(successResponse);
    vi.mocked(restoreWarehouseDatabase).mockResolvedValue(successResponse);
  });

  it('viser kerneelementer efter initial indlæsning', async () => {
    render(<WarehousePage />);

    await screen.findByRole('heading', { name: 'Palle sortering' });

    expect(screen.getByRole('button', { name: 'Skift til simpel scanner-visning' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Registrer kolli' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Bekræft flytning' })).toBeInTheDocument();
  });

  it('kan skifte til simpel scanner-visning og skjule avancerede sektioner', async () => {
    vi.mocked(fetchWarehouseDashboard).mockResolvedValue({
      openPallets: [
        {
          palletId: 'P-001',
          groupKey: 'A|20260101',
          productNumber: 'A',
          expiryDate: '20260101',
          totalQuantity: 2,
          isClosed: false,
          createdAt: new Date().toISOString(),
        },
      ],
      entries: [
        {
          id: 1,
          timestamp: new Date().toISOString(),
          productNumber: 'A',
          expiryDate: '20260101',
          quantity: 2,
          palletId: 'P-001',
          groupKey: 'A|20260101',
          createdNewPallet: true,
          confirmedQuantity: 0,
          confirmedMoved: false,
        },
      ],
    });

    render(<WarehousePage />);
    const user = userEvent.setup();

    await screen.findByRole('button', { name: 'Fortryd seneste' });
    await user.click(screen.getByRole('button', { name: 'Skift til simpel scanner-visning' }));

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: 'Fortryd seneste' })).not.toBeInTheDocument();
    });
  });

  it('sender registreringsdata til API ved submit', async () => {
    render(<WarehousePage />);
    const user = userEvent.setup();

    await screen.findByRole('heading', { name: 'Registrer kolli' });

    await user.type(screen.getByLabelText('Varenummer'), 'ABC-100');
    await user.type(screen.getByLabelText('Holdbarhed (YYYYMMDD)'), '20261231');
    fireEvent.change(screen.getByLabelText('Antal kolli'), { target: { value: '3' } });
    await user.click(screen.getByRole('button', { name: 'Registrer kolli' }));

    await waitFor(() => {
      expect(registerWarehouseColli).toHaveBeenCalledWith('ABC-100', '20261231', 3);
    });
  });
});
