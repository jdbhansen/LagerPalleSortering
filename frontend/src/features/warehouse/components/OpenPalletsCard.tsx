import type { WarehousePalletRecord } from '../models';
import { navigateTo } from '../../../navigation';

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
                    <button
                      className="btn btn-sm btn-outline-primary"
                      type="button"
                      onClick={() => navigateTo(`/app/print-label/${encodeURIComponent(pallet.palletId)}`)}
                    >
                      Label
                    </button>
                    <button
                      className="btn btn-sm btn-outline-dark"
                      type="button"
                      onClick={() => navigateTo(`/app/print-pallet-contents/${encodeURIComponent(pallet.palletId)}`)}
                    >
                      Indhold
                    </button>
                    <button
                      className="btn btn-sm btn-outline-dark"
                      type="button"
                      onClick={() => navigateTo(`/app/print-pallet-contents/${encodeURIComponent(pallet.palletId)}?format=label190x100`)}
                    >
                      Indhold 190x100
                    </button>
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
