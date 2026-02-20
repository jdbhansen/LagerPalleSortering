import './App.css';
import { NewPalletSortingPage } from './features/warehouse/NewPalletSortingPage';
import { WarehousePage } from './features/warehouse/WarehousePage';
import { PrintLabelPage } from './features/warehouse/print/PrintLabelPage';
import { PrintPalletContentsPage } from './features/warehouse/print/PrintPalletContentsPage';
import { useEffect, useState } from 'react';
import { subscribeNavigation } from './navigation';
import { warehouseStorageKeys } from './features/warehouse/constants';
import type { WarehouseViewMode } from './features/warehouse/constants';
import { getWarehousePrintRoute } from './features/warehouse/warehouseRouting';

function App() {
  const [locationState, setLocationState] = useState(() => ({
    pathname: window.location.pathname,
    search: window.location.search,
  }));

  useEffect(() => {
    return subscribeNavigation(() => {
      setLocationState({
        pathname: window.location.pathname,
        search: window.location.search,
      });
    });
  }, []);

  const printRoute = getWarehousePrintRoute(locationState.pathname, locationState.search);
  const [viewMode, setViewMode] = useState<WarehouseViewMode>(() => {
    const stored = window.localStorage.getItem(warehouseStorageKeys.viewMode);
    return stored === 'fullOverview' ? 'fullOverview' : 'newSorting';
  });

  useEffect(() => {
    window.localStorage.setItem(warehouseStorageKeys.viewMode, viewMode);
  }, [viewMode]);

  if (printRoute?.type === 'label') {
    return <PrintLabelPage palletId={printRoute.palletId} />;
  }

  if (printRoute?.type === 'contents') {
    return <PrintPalletContentsPage palletId={printRoute.palletId} format={printRoute.format ?? null} />;
  }

  return (
    <>
      <header className="mode-switcher">
        <div className="container-xl mode-switcher-inner">
          <span className="mode-switcher-label">Tilstand</span>
          <div className="btn-group" role="group" aria-label="Skift tilstand">
            <button
              type="button"
              className={`btn ${viewMode === 'newSorting' ? 'btn-primary' : 'btn-outline-primary'}`}
              onClick={() => setViewMode('newSorting')}
            >
              Ny pallesortering
            </button>
            <button
              type="button"
              className={`btn ${viewMode === 'fullOverview' ? 'btn-primary' : 'btn-outline-primary'}`}
              onClick={() => setViewMode('fullOverview')}
            >
              Fuld oversigt
            </button>
          </div>
        </div>
      </header>

      {viewMode === 'newSorting' ? <NewPalletSortingPage /> : <WarehousePage />}
    </>
  );
}

export default App;
