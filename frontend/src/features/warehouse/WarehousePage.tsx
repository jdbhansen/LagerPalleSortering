import { ClearDatabaseWarning } from './components/ClearDatabaseWarning';
import { ConfirmMoveCard } from './components/ConfirmMoveCard';
import { OpenPalletsCard } from './components/OpenPalletsCard';
import { PalletContentsOverviewCard } from './components/PalletContentsOverviewCard';
import { RecentEntriesCard } from './components/RecentEntriesCard';
import { RegisterColliCard } from './components/RegisterColliCard';
import { RestoreDatabaseCard } from './components/RestoreDatabaseCard';
import { WarehouseHeader } from './components/WarehouseHeader';
import { WarehouseStatsCards } from './components/WarehouseStatsCards';
import { WarehouseStatusAlert } from './components/WarehouseStatusAlert';
import { useWarehousePage } from './hooks/useWarehousePage';

export function WarehousePage() {
  const {
    loading,
    dashboard,
    isSimpleMode,
    status,
    showClearWarning,
    lastSuggestedPalletId,
    openColli,
    pendingConfirmations,
    registerForm,
    confirmForm,
    restoreFile,
    setIsSimpleMode,
    setRestoreFile,
    updateRegisterFormField,
    updateConfirmFormField,
    reportClientError,
    submitRegisterColli,
    submitConfirmMove,
    closePallet,
    undoLastEntry,
    openClearDatabaseWarning,
    cancelClearDatabaseWarning,
    clearDatabase,
    restoreDatabase,
  } = useWarehousePage();

  if (loading) {
    return <main className="container-xl py-4">Indlæser...</main>;
  }

  return (
    <main className="container-xl py-4">
      <WarehouseHeader
        isSimpleMode={isSimpleMode}
        onToggleSimpleMode={() => setIsSimpleMode((previous) => !previous)}
        onOpenClearDatabaseWarning={openClearDatabaseWarning}
      />

      <WarehouseStatsCards
        openPalletCount={dashboard.openPallets.length}
        openColliCount={openColli}
        pendingConfirmationCount={pendingConfirmations}
      />

      <ClearDatabaseWarning
        visible={showClearWarning}
        onConfirm={() => {
          clearDatabase().catch(reportClientError);
        }}
        onCancel={cancelClearDatabaseWarning}
      />

      <WarehouseStatusAlert status={status} />

      <div className="row g-3 mb-3">
        <div className="col-12 col-lg-6">
          <RegisterColliCard
            productNumber={registerForm.productNumber}
            expiryDateRaw={registerForm.expiryDateRaw}
            quantity={registerForm.quantity}
            onProductNumberChange={(value) => updateRegisterFormField('productNumber', value)}
            onExpiryDateChange={(value) => updateRegisterFormField('expiryDateRaw', value)}
            onQuantityChange={(value) => updateRegisterFormField('quantity', value)}
            onSubmit={submitRegisterColli}
            onError={reportClientError}
          />
        </div>

        <div className="col-12 col-lg-6">
          <ConfirmMoveCard
            scannedPalletCode={confirmForm.scannedPalletCode}
            confirmScanCount={confirmForm.confirmScanCount}
            lastSuggestedPalletId={lastSuggestedPalletId}
            onScannedPalletCodeChange={(value) => updateConfirmFormField('scannedPalletCode', value)}
            onConfirmScanCountChange={(value) => updateConfirmFormField('confirmScanCount', value)}
            onSubmit={submitConfirmMove}
            onError={reportClientError}
          />
        </div>
      </div>

      {!isSimpleMode && (
        <>
          <OpenPalletsCard
            pallets={dashboard.openPallets}
            onClosePallet={closePallet}
            onError={reportClientError}
          />

          <PalletContentsOverviewCard
            pallets={dashboard.openPallets}
            onError={reportClientError}
          />

          <RecentEntriesCard
            entries={dashboard.entries}
            onUndoLastEntry={undoLastEntry}
            onError={reportClientError}
          />

          <RestoreDatabaseCard
            restoreFile={restoreFile}
            onRestoreFileChange={setRestoreFile}
            onRestoreDatabase={restoreDatabase}
            onError={reportClientError}
          />
        </>
      )}
    </main>
  );
}
