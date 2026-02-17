import type { WarehousePalletRecord } from '../models';

interface OpenPalletsCardProps {
  pallets: WarehousePalletRecord[];
  onClosePallet: (palletId: string, printContents: boolean) => Promise<void>;
  onError: (error: unknown) => void;
}

export function OpenPalletsCard({ pallets, onClosePallet, onError }: OpenPalletsCardProps) {
  return (
    <div className="card border-0 shadow-sm mb-3">
      <div className="card-header d-flex justify-content-between align-items-center gap-2 bg-body">
        <span className="fw-semibold">Åbne paller</span>
        <span className="badge text-bg-secondary">{pallets.length} stk</span>
      </div>
      <div className="table-responsive">
        <table className="table align-middle mb-0">
          <thead className="table-light">
            <tr>
              <th>Palle</th>
              <th>Varenummer</th>
              <th>Holdbarhed</th>
              <th>Kolli</th>
              <th className="text-end">Handling</th>
            </tr>
          </thead>
          <tbody>
            {pallets.length === 0 && (
              <tr>
                <td colSpan={5} className="text-muted">Ingen åbne paller endnu.</td>
              </tr>
            )}
            {pallets.map((pallet) => (
              <tr key={pallet.palletId}>
                <td className="fw-semibold">{pallet.palletId}</td>
                <td>{pallet.productNumber}</td>
                <td>{pallet.expiryDate}</td>
                <td>{pallet.totalQuantity}</td>
                <td className="text-end">
                  <div className="d-inline-flex flex-wrap gap-1 justify-content-end">
                    <a className="btn btn-sm btn-outline-primary" href={`/print-label/${encodeURIComponent(pallet.palletId)}`} target="_blank" rel="noreferrer">Label</a>
                    <a className="btn btn-sm btn-outline-dark" href={`/print-pallet-contents/${encodeURIComponent(pallet.palletId)}`} target="_blank" rel="noreferrer">Indhold</a>
                    <button
                      className="btn btn-sm btn-primary"
                      type="button"
                      onClick={() => onClosePallet(pallet.palletId, true).catch(onError)}
                    >
                      Luk + print
                    </button>
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      type="button"
                      onClick={() => onClosePallet(pallet.palletId, false).catch(onError)}
                    >
                      Luk
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
