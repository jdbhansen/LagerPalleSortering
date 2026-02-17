interface WarehouseStatsProps {
  openPalletCount: number;
  openColliCount: number;
  pendingConfirmationCount: number;
}

export function WarehouseStatsCards({
  openPalletCount,
  openColliCount,
  pendingConfirmationCount,
}: WarehouseStatsProps) {
  return (
    <div className="row g-3 mb-3">
      <div className="col-12 col-md-4">
        <div className="card border-0 shadow-sm h-100">
          <div className="card-body py-3">
            <div className="text-muted small">Åbne paller</div>
            <div className="fs-3 fw-semibold">{openPalletCount}</div>
          </div>
        </div>
      </div>
      <div className="col-12 col-md-4">
        <div className="card border-0 shadow-sm h-100">
          <div className="card-body py-3">
            <div className="text-muted small">Åbne kolli</div>
            <div className="fs-3 fw-semibold">{openColliCount}</div>
          </div>
        </div>
      </div>
      <div className="col-12 col-md-4">
        <div className="card border-0 shadow-sm h-100">
          <div className="card-body py-3">
            <div className="text-muted small">Afventer scan</div>
            <div className="fs-3 fw-semibold">{pendingConfirmationCount}</div>
          </div>
        </div>
      </div>
    </div>
  );
}
