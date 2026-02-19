import { useEffect, useState } from 'react';
import { fetchWarehousePalletContents } from '../api/warehouseApiClient';
import type { WarehousePalletContentItemRecord, WarehousePalletRecord } from '../models';
import { formatExpiryDateForDisplay } from '../utils/expiryDate';

interface PalletContentsOverviewCardProps {
  pallets: WarehousePalletRecord[];
  additionalPalletIds?: string[];
  onError: (error: unknown) => void;
  onClosePallet?: (palletId: string) => Promise<void>;
  onPrintLabel?: (palletId: string) => void;
  closingPallet?: boolean;
}

export function PalletContentsOverviewCard({
  pallets,
  additionalPalletIds = [],
  onError,
  onClosePallet,
  onPrintLabel,
  closingPallet = false,
}: PalletContentsOverviewCardProps) {
  const [selectedPalletId, setSelectedPalletId] = useState('');
  const [items, setItems] = useState<WarehousePalletContentItemRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const openPalletIds = new Set(pallets.map((pallet) => pallet.palletId));
  const palletOptions = Array.from(new Set([...pallets.map((pallet) => pallet.palletId), ...additionalPalletIds]));

  useEffect(() => {
    if (palletOptions.length === 0) {
      setSelectedPalletId('');
      setItems([]);
      return;
    }

    const stillExists = palletOptions.includes(selectedPalletId);
    if (!stillExists) {
      setSelectedPalletId(palletOptions[0]);
    }
  }, [palletOptions, selectedPalletId]);

  useEffect(() => {
    if (!selectedPalletId) {
      setItems([]);
      return;
    }

    let active = true;

    async function loadContents() {
      setLoading(true);
      try {
        const response = await fetchWarehousePalletContents(selectedPalletId);
        if (active) {
          setItems(response.items);
        }
      } catch (error: unknown) {
        if (active) {
          onError(error);
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void loadContents();

    return () => {
      active = false;
    };
  }, [onError, selectedPalletId]);

  return (
    <div className="card border-0 shadow-sm mb-3">
      <div className="card-header d-flex justify-content-between align-items-center bg-body">
        <span className="fw-semibold">Indhold på paller</span>
        <span className="badge text-bg-secondary">{pallets.length} åbne</span>
      </div>
      <div className="card-body">
        {palletOptions.length === 0 ? (
          <p className="text-muted mb-0">Ingen åbne paller at vise endnu.</p>
        ) : (
          <>
            <div className="row g-2 align-items-end mb-3">
              <div className="col-12 col-md-6">
                <label className="form-label mb-1" htmlFor="pallet-contents-select">Vælg palle</label>
                <select
                  id="pallet-contents-select"
                  className="form-select"
                  value={selectedPalletId}
                  onChange={(event) => setSelectedPalletId(event.target.value)}
                >
                  {palletOptions.map((palletId) => (
                    <option key={palletId} value={palletId}>
                      {palletId}{openPalletIds.has(palletId) ? ' (åben)' : ' (lukket)'}
                    </option>
                  ))}
                </select>
              </div>
              {onPrintLabel && (
                <div className="col-12 col-md-6 d-flex justify-content-md-end">
                  <button
                    className="btn btn-outline-primary me-2"
                    type="button"
                    disabled={!selectedPalletId}
                    onClick={() => onPrintLabel(selectedPalletId)}
                  >
                    Print indholdslabel igen
                  </button>
                </div>
              )}
              {onClosePallet && (
                <div className="col-12 col-md-6 d-flex justify-content-md-end">
                  <button
                    className="btn btn-outline-secondary"
                    type="button"
                    disabled={!selectedPalletId || closingPallet || !openPalletIds.has(selectedPalletId)}
                    onClick={() => {
                      void onClosePallet(selectedPalletId);
                    }}
                  >
                    Luk valgt palle
                  </button>
                </div>
              )}
            </div>

            <div className="table-responsive">
              <table className="table align-middle mb-0">
                <thead className="table-light">
                  <tr>
                    <th>Varenummer</th>
                    <th>Holdbarhed</th>
                    <th>Antal</th>
                  </tr>
                </thead>
                <tbody>
                  {loading && (
                    <tr>
                      <td colSpan={3} className="text-muted">Indlæser palleindhold...</td>
                    </tr>
                  )}
                  {!loading && items.length === 0 && (
                    <tr>
                      <td colSpan={3} className="text-muted">Ingen varelinjer på den valgte palle.</td>
                    </tr>
                  )}
                  {!loading && items.map((item) => (
                    <tr key={`${item.productNumber}-${item.expiryDate}`}>
                      <td>{item.productNumber}</td>
                      <td>{formatExpiryDateForDisplay(item.expiryDate)}</td>
                      <td>{item.quantity}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
