import type { FormEvent } from 'react';
import { PalletContentsOverviewCard } from './components/PalletContentsOverviewCard';
import { WarehouseStatsCards } from './components/WarehouseStatsCards';
import { WarehouseStatusAlert } from './components/WarehouseStatusAlert';
import { useNewPalletSorting } from './hooks/useNewPalletSorting';
import { usePrintPreferences } from './hooks/usePrintPreferences';
import { openPrinterSetupDialog } from './print/openPrinterSetupDialog';

export function NewPalletSortingPage() {
  const {
    loading,
    started,
    activeStep,
    submitting,
    dashboard,
    status,
    productNumber,
    expiryDateRaw,
    scannedPalletCode,
    suggestedPalletId,
    openColli,
    pendingConfirmations,
    recentClosedPalletIds,
    palletContentsRefreshToken,
    productInputRef,
    palletInputRef,
    setProductNumber,
    setExpiryDateRaw,
    setScannedPalletCode,
    startNewSorting,
    finishSorting,
    registerOneColli,
    confirmMove,
    closeCurrentPalletAndPrintLabel,
    closePalletFromList,
    printPalletContentsLabel,
    reportClientError,
  } = useNewPalletSorting();
  const {
    preferredPrinterName,
    setPreferredPrinterName,
  } = usePrintPreferences();

  if (loading) {
    return <main className="container-xl py-4">Indlæser...</main>;
  }

  return (
    <main className="container-xl py-4">
      <section className="scanner-flow-header mb-3">
        <h1 className="mb-1">Ny pallesortering</h1>
        <p className="text-muted mb-0">1) Scan kolli stregkode 2) Indtast holdbarhed 3) Scan pallelabel for at sætte kollien på plads.</p>
        <div className="mt-3 p-3 border rounded bg-body-tertiary">
          <div className="d-flex flex-wrap align-items-center gap-2">
            <button className="btn btn-outline-primary" type="button" onClick={openPrinterSetupDialog}>
              Vælg/skift printer
            </button>
            <div className="small text-muted">
              Aktiv printer: <strong>{preferredPrinterName.trim() || 'Ikke angivet'}</strong>
            </div>
          </div>
          <div className="mt-2">
            <label htmlFor="preferred-printer-name" className="form-label mb-1">Printer navn (vises for operatøren)</label>
            <input
              id="preferred-printer-name"
              type="text"
              className="form-control"
              placeholder="Fx Zebra ZD421"
              value={preferredPrinterName}
              onChange={(event) => setPreferredPrinterName(event.target.value)}
            />
          </div>
        </div>
        {started && (
          <div className="mt-2">
            <button className="btn btn-outline-danger" type="button" onClick={finishSorting} disabled={submitting}>
              Afslut pallesortering
            </button>
          </div>
        )}
      </section>

      <WarehouseStatsCards
        openPalletCount={dashboard.openPallets.length}
        openColliCount={openColli}
        pendingConfirmationCount={pendingConfirmations}
      />

      {!started ? (
        <div className="card border-0 shadow-sm scanner-flow-start">
          <div className="card-body d-flex flex-column gap-3">
            <p className="mb-0">Start en ny sortering, så aktiveres scannerflowet for én kolli ad gangen.</p>
            <div>
              <button className="btn btn-primary btn-lg" type="button" onClick={startNewSorting}>Start ny pallesortering</button>
            </div>
          </div>
        </div>
      ) : (
        <div className="row g-3">
          <div className="col-12">
            <div className="mb-2 small text-uppercase text-muted fw-semibold">
              Aktivt trin: {activeStep === 'register' ? '1 af 2' : '2 af 2'}
            </div>
            {activeStep === 'register' ? (
              <div className="card border-0 shadow-sm">
                <div className="card-header bg-body fw-semibold">Trin 1: Kolli + holdbarhed</div>
                <div className="card-body">
                  <form onSubmit={(event: FormEvent<HTMLFormElement>) => { event.preventDefault(); void registerOneColli(); }}>
                    <div className="mb-3">
                      <label className="form-label" htmlFor="new-sort-product">Kolli stregkode</label>
                      <input
                        id="new-sort-product"
                        ref={productInputRef}
                        className="form-control form-control-lg"
                        autoComplete="off"
                        value={productNumber}
                        onChange={(event) => setProductNumber(event.target.value)}
                        required
                      />
                    </div>
                    <div className="mb-3">
                      <label className="form-label" htmlFor="new-sort-expiry">Holdbarhed (YYYYMMDD)</label>
                      <input
                        id="new-sort-expiry"
                        className="form-control form-control-lg"
                        autoComplete="off"
                        value={expiryDateRaw}
                        onChange={(event) => setExpiryDateRaw(event.target.value)}
                        inputMode="numeric"
                        pattern="\d{8}"
                        required
                      />
                    </div>
                    <button className="btn btn-primary" type="submit" disabled={submitting}>Registrer kolli</button>
                  </form>
                </div>
              </div>
            ) : (
              <div className="card border-0 shadow-sm">
                <div className="card-header bg-body fw-semibold">Trin 2: Scan palle</div>
                <div className="card-body">
                  <form onSubmit={(event: FormEvent<HTMLFormElement>) => { event.preventDefault(); void confirmMove(); }}>
                    <div className="mb-2 small text-muted">
                      Foreslået palle: <strong>{suggestedPalletId || '-'}</strong>
                    </div>
                    <div className="mb-3">
                      <label className="form-label" htmlFor="new-sort-pallet">Palle stregkode</label>
                      <input
                        id="new-sort-pallet"
                        ref={palletInputRef}
                        className="form-control form-control-lg"
                        autoComplete="off"
                        value={scannedPalletCode}
                        onChange={(event) => setScannedPalletCode(event.target.value)}
                        placeholder={suggestedPalletId ? `PALLET:${suggestedPalletId}` : 'PALLET:P-001'}
                      />
                    </div>
                    <button className="btn btn-success" type="submit" disabled={submitting}>
                      Sæt kolli på plads
                    </button>
                    <button
                      className="btn btn-outline-dark ms-2"
                      type="button"
                      disabled={submitting || !suggestedPalletId}
                      onClick={() => {
                        void closeCurrentPalletAndPrintLabel();
                      }}
                    >
                      Luk palle + print indholdslabel
                    </button>
                  </form>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      <div className="mt-3">
        <WarehouseStatusAlert status={status} />
      </div>

      <div className="mt-3">
        <PalletContentsOverviewCard
          pallets={dashboard.openPallets}
          additionalPalletIds={recentClosedPalletIds}
          refreshToken={palletContentsRefreshToken}
          onError={reportClientError}
          onClosePallet={closePalletFromList}
          onPrintLabel={printPalletContentsLabel}
          closingPallet={submitting}
        />
      </div>
    </main>
  );
}
