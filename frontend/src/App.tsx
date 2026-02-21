import './App.css';
import { NewPalletSortingPage } from './features/warehouse/NewPalletSortingPage';
import { WarehousePage } from './features/warehouse/WarehousePage';
import { PrintLabelPage } from './features/warehouse/print/PrintLabelPage';
import { PrintPalletContentsPage } from './features/warehouse/print/PrintPalletContentsPage';
import { useEffect, useState } from 'react';
import { navigateTo, subscribeNavigation } from './navigation';
import { warehouseStorageKeys } from './features/warehouse/constants';
import type { WarehouseViewMode } from './features/warehouse/constants';
import { getWarehousePrintRoute } from './features/warehouse/warehouseRouting';
import { LoginPage } from './features/auth/LoginPage';

interface AuthState {
  loading: boolean;
  authenticated: boolean;
  username: string;
}

async function fetchAuthState(): Promise<AuthState> {
  try {
    const response = await fetch('/auth/me', { credentials: 'include' });
    if (!response.ok) {
      return {
        loading: false,
        authenticated: false,
        username: '',
      };
    }

    const payload = (await response.json()) as { authenticated?: boolean; username?: string };
    return {
      loading: false,
      authenticated: payload.authenticated === true,
      username: payload.username ?? '',
    };
  } catch {
    return {
      loading: false,
      authenticated: false,
      username: '',
    };
  }
}

function App() {
  const [locationState, setLocationState] = useState(() => ({
    pathname: window.location.pathname,
    search: window.location.search,
  }));
  const [authState, setAuthState] = useState<AuthState>({
    loading: true,
    authenticated: false,
    username: '',
  });

  useEffect(() => {
    return subscribeNavigation(() => {
      setLocationState({
        pathname: window.location.pathname,
        search: window.location.search,
      });
    });
  }, []);

  async function refreshAuthState() {
    const nextState = await fetchAuthState();
    setAuthState(nextState);
  }

  useEffect(() => {
    let active = true;
    void fetchAuthState().then((nextState) => {
      if (active) {
        setAuthState(nextState);
      }
    });

    return () => {
      active = false;
    };
  }, []);

  const printRoute = getWarehousePrintRoute(locationState.pathname, locationState.search);
  const [viewMode, setViewMode] = useState<WarehouseViewMode>(() => {
    const stored = window.localStorage.getItem(warehouseStorageKeys.viewMode);
    return stored === 'fullOverview' ? 'fullOverview' : 'newSorting';
  });

  useEffect(() => {
    window.localStorage.setItem(warehouseStorageKeys.viewMode, viewMode);
  }, [viewMode]);

  async function logout() {
    await fetch('/auth/logout', {
      method: 'POST',
      credentials: 'include',
    });

    await refreshAuthState();
    navigateTo('/login', true);
  }

  if (authState.loading) {
    return <main className="container-xl py-4">Indlæser...</main>;
  }

  if (!authState.authenticated) {
    return (
      <LoginPage
        onLoginSuccess={async () => {
          await refreshAuthState();
          navigateTo('/app', true);
        }}
      />
    );
  }

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
          <div className="ms-auto d-flex align-items-center gap-2">
            <span className="small text-muted">Logget ind som {authState.username}</span>
            <button type="button" className="btn btn-outline-secondary btn-sm" onClick={() => { void logout(); }}>
              Log ud
            </button>
          </div>
        </div>
      </header>

      {viewMode === 'newSorting' ? <NewPalletSortingPage /> : <WarehousePage />}
    </>
  );
}

export default App;
