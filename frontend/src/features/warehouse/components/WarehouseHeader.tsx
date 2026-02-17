interface WarehouseHeaderProps {
  isSimpleMode: boolean;
  onToggleSimpleMode: () => void;
  onOpenClearDatabaseWarning: () => void;
}

export function WarehouseHeader({
  isSimpleMode,
  onToggleSimpleMode,
  onOpenClearDatabaseWarning,
}: WarehouseHeaderProps) {
  return (
    <div className="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-3">
      <div>
        <h1 className="mb-1">Palle sortering</h1>
        <p className="text-muted mb-0">1) Registrer kolli 2) Flyt til foreslået palle 3) Scan pallelabel for bekræftelse.</p>
      </div>
      <div className="d-flex flex-wrap gap-2">
        <button className="btn btn-primary" type="button" onClick={onToggleSimpleMode}>
          {isSimpleMode ? 'Skift til avanceret visning' : 'Skift til simpel scanner-visning'}
        </button>
        {!isSimpleMode && (
          <>
            <a className="btn btn-outline-success" href="/export/csv">Eksport CSV</a>
            <a className="btn btn-outline-success" href="/export/excel">Eksport Excel</a>
            <a className="btn btn-outline-secondary" href="/backup/db">Backup DB</a>
            <button className="btn btn-outline-danger" type="button" onClick={onOpenClearDatabaseWarning}>
              Ryd database
            </button>
          </>
        )}
      </div>
    </div>
  );
}
