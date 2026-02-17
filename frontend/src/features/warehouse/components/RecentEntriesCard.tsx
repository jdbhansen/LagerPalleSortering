import type { WarehouseScanEntryRecord } from '../models';
import { ScanBarcodeSvg } from './ScanBarcodeSvg';
import { formatExpiryDateForDisplay } from '../utils/expiryDate';

interface RecentEntriesCardProps {
  entries: WarehouseScanEntryRecord[];
  onUndoLastEntry: () => Promise<void>;
  onError: (error: unknown) => void;
}

export function RecentEntriesCard({ entries, onUndoLastEntry, onError }: RecentEntriesCardProps) {
  return (
    <div className="card border-0 shadow-sm mb-3">
      <div className="card-header d-flex justify-content-between align-items-center bg-body">
        <span className="fw-semibold">Seneste registreringer</span>
        <button
          className="btn btn-sm btn-outline-danger"
          type="button"
          disabled={entries.length === 0}
          onClick={() => onUndoLastEntry().catch(onError)}
        >
          Fortryd seneste
        </button>
      </div>
      <div className="table-responsive">
        <table className="table align-middle mb-0">
          <thead className="table-light">
            <tr>
              <th>Tid</th>
              <th>Vare</th>
              <th>Dato</th>
              <th>Datostregkode</th>
              <th>Kolli</th>
              <th>Palle</th>
              <th>Status</th>
              <th>Handling</th>
            </tr>
          </thead>
          <tbody>
            {entries.length === 0 && (
              <tr>
                <td colSpan={8} className="text-muted">Ingen registreringer endnu.</td>
              </tr>
            )}
            {entries.map((entry) => {
              const timestamp = new Date(entry.timestamp).toLocaleTimeString('da-DK', { hour12: false });
              const hasScannableExpiry = /^\d{8}$/.test(entry.expiryDate);
              const expiryDateDisplay = formatExpiryDateForDisplay(entry.expiryDate);

              return (
                <tr key={entry.id}>
                  <td>{timestamp}</td>
                  <td>{entry.productNumber}</td>
                  <td>{expiryDateDisplay}</td>
                  <td>
                    {hasScannableExpiry ? (
                      <div className="d-flex flex-column align-items-center">
                        <span className="badge text-bg-info mb-1">DATO</span>
                        <ScanBarcodeSvg value={entry.expiryDate} width={1} height={28} displayValue={false} />
                      </div>
                    ) : (
                      <span className="text-muted">-</span>
                    )}
                  </td>
                  <td>{entry.quantity}</td>
                  <td>{entry.palletId}</td>
                  <td>
                    {entry.confirmedMoved ? (
                      <span className="badge text-bg-success">Bekræftet ({entry.confirmedQuantity}/{entry.quantity})</span>
                    ) : (
                      <span className="badge text-bg-warning">Afventer ({entry.confirmedQuantity}/{entry.quantity})</span>
                    )}
                  </td>
                  <td>
                    <div className="d-inline-flex flex-wrap gap-1">
                      <a
                        className="btn btn-sm btn-outline-dark"
                        href={`/print-pallet-contents/${encodeURIComponent(entry.palletId)}`}
                        target="_blank"
                        rel="noreferrer"
                      >
                        Print indhold
                      </a>
                      <a
                        className="btn btn-sm btn-outline-dark"
                        href={`/print-pallet-contents/${encodeURIComponent(entry.palletId)}?format=label190x100`}
                        target="_blank"
                        rel="noreferrer"
                      >
                        190x100
                      </a>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
