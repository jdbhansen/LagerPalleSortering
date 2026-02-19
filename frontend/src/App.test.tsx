import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
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
    window.localStorage.clear();
    window.history.replaceState({}, '', '/app');
  });

  it('viser ny pallesortering som default', () => {
    render(<App />);
    expect(screen.getByText('new-sorting-view')).toBeInTheDocument();
  });

  it('bruger gemt visningstilstand fra localStorage', () => {
    window.localStorage.setItem(warehouseStorageKeys.viewMode, 'fullOverview');
    render(<App />);
    expect(screen.getByText('full-overview-view')).toBeInTheDocument();
  });

  it('kan skifte tilstand og gemmer valget', () => {
    render(<App />);
    fireEvent.click(screen.getByRole('button', { name: 'Fuld oversigt' }));

    expect(screen.getByText('full-overview-view')).toBeInTheDocument();
    expect(window.localStorage.getItem(warehouseStorageKeys.viewMode)).toBe('fullOverview');
  });

  it('renderer print label route i stedet for hovedvisning', () => {
    window.history.replaceState({}, '', '/app/print-label/P-100');
    render(<App />);
    expect(screen.getByText('print-label:P-100')).toBeInTheDocument();
  });
});
