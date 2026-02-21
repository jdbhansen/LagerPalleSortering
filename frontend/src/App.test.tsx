import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { warehouseStorageKeys } from './features/warehouse/constants';
import App from './App';

vi.mock('./features/warehouse/NewPalletSortingPage', () => ({
  NewPalletSortingPage: () => <div>new-sorting-view</div>,
}));

vi.mock('./features/warehouse/WarehousePage', () => ({
  WarehousePage: () => <div>full-overview-view</div>,
}));

vi.mock('./features/warehouse/print/PrintLabelPage', () => ({
  PrintLabelPage: ({ palletId }: { palletId: string }) => <div>print-label:{palletId}</div>,
}));

vi.mock('./features/warehouse/print/PrintPalletContentsPage', () => ({
  PrintPalletContentsPage: ({ palletId, format }: { palletId: string; format: string | null }) => (
    <div>print-contents:{palletId}:{format ?? 'none'}</div>
  ),
}));

describe('App', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ authenticated: true, username: 'tester' }),
    }));
    window.localStorage.clear();
    window.history.replaceState({}, '', '/app');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('viser ny pallesortering som default', async () => {
    render(<App />);
    expect(await screen.findByText('new-sorting-view')).toBeInTheDocument();
  });

  it('bruger gemt visningstilstand fra localStorage', async () => {
    window.localStorage.setItem(warehouseStorageKeys.viewMode, 'fullOverview');
    render(<App />);
    expect(await screen.findByText('full-overview-view')).toBeInTheDocument();
  });

  it('kan skifte tilstand og gemmer valget', async () => {
    render(<App />);
    fireEvent.click(await screen.findByRole('button', { name: 'Fuld oversigt' }));

    expect(await screen.findByText('full-overview-view')).toBeInTheDocument();
    expect(window.localStorage.getItem(warehouseStorageKeys.viewMode)).toBe('fullOverview');
  });

  it('renderer print label route i stedet for hovedvisning', async () => {
    window.history.replaceState({}, '', '/app/print-label/P-100');
    render(<App />);
    expect(await screen.findByText('print-label:P-100')).toBeInTheDocument();
  });
});
