interface RestoreDatabaseCardProps {
  restoreFile: File | null;
  onRestoreFileChange: (file: File | null) => void;
  onRestoreDatabase: () => Promise<void>;
  onError: (error: unknown) => void;
}

export function RestoreDatabaseCard({
  restoreFile,
  onRestoreFileChange,
  onRestoreDatabase,
  onError,
}: RestoreDatabaseCardProps) {
  return (
    <div className="card border-0 shadow-sm">
      <div className="card-header bg-body fw-semibold">Database restore</div>
      <div className="card-body">
        <div className="row g-2 align-items-end">
          <div className="col-12 col-md-8">
            <label className="form-label">Vælg backupfil (.db)</label>
            <input
              className="form-control"
              type="file"
              accept=".db,.sqlite,.sqlite3"
              onChange={(event) => onRestoreFileChange(event.target.files?.[0] ?? null)}
            />
          </div>
          <div className="col-12 col-md-4 d-grid">
            <button
              className="btn btn-outline-warning"
              type="button"
              disabled={!restoreFile}
              onClick={() => onRestoreDatabase().catch(onError)}
            >
              Gendan database
            </button>
          </div>
        </div>
        {restoreFile && <div className="small text-muted mt-2">Valgt fil: {restoreFile.name}</div>}
      </div>
    </div>
  );
}
