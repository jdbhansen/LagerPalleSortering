import { useEffect, useMemo, useState } from 'react';
import type { WarehousePalletContentItemRecord } from '../models';
import { fetchWarehousePalletContents } from '../api/warehouseApiClient';
import { ScanBarcodeSvg } from '../components/ScanBarcodeSvg';
import { formatExpiryDateForDisplay } from '../utils/expiryDate';
import { navigateTo } from '../../../navigation';
import { usePrintOnMount } from '../hooks/usePrintOnMount';

interface PrintPalletContentsPageProps {
  palletId: string;
  format: string | null;
}

export function PrintPalletContentsPage({ palletId, format }: PrintPalletContentsPageProps) {
  const [items, setItems] = useState<WarehousePalletContentItemRecord[]>([]);
  const [error, setError] = useState<string | null>(null);

  const isLabel190x100 = useMemo(
    () => String(format ?? '').toLowerCase() === 'label190x100',
    [format],
  );

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const response = await fetchWarehousePalletContents(palletId);
        if (active) {
          setItems(response.items);
        }
      } catch (loadError: unknown) {
        if (active) {
          const message = loadError instanceof Error ? loadError.message : 'Kunne ikke hente palleindhold.';
          setError(message);
        }
      }
    }

    void load();
    return () => {
      active = false;
    };
  }, [palletId]);

  usePrintOnMount(!error);

  return (
    <main className="print-page print-contents-page">
      <section className={`print-sheet ${isLabel190x100 ? 'print-contents-sheet-190x100' : 'print-contents-sheet'}`}>
        <h1 className="print-title">Palle indhold</h1>
        <div className="print-pallet-id">Palle: {palletId}</div>

        {error ? (
          <div className="alert alert-danger" role="alert">{error}</div>
        ) : items.length === 0 ? (
          <div className="text-muted">Ingen indholdslinjer fundet.</div>
        ) : (
          <div className="d-grid gap-3">
            {items.map((item, index) => {
              const expiryDisplay = formatExpiryDateForDisplay(item.expiryDate);
              const scannableExpiry = /^\d{8}$/.test(item.expiryDate);

              return (
                <article key={`${item.productNumber}-${item.expiryDate}-${index}`} className="print-item-row">
                  <div className="fw-semibold">Vare: {item.productNumber}</div>
                  <div>Holdbarhed: {expiryDisplay}</div>
                  <div>Antal: {item.quantity}</div>
                  <div className="print-item-barcode mt-2">
                    <ScanBarcodeSvg value={item.productNumber} width={1.6} height={58} displayValue={false} />
                  </div>
                  <div className="small text-muted text-center">{item.productNumber}</div>
                  {scannableExpiry && (
                    <>
                      <div className="small text-uppercase fw-semibold text-secondary text-center mt-1">Dato / Holdbarhed</div>
                      <div className="print-item-barcode">
                        <ScanBarcodeSvg value={item.expiryDate} width={1.2} height={42} displayValue={false} />
                      </div>
                      <div className="small text-muted text-center">{expiryDisplay}</div>
                    </>
                  )}
                </article>
              );
            })}
          </div>
        )}
      </section>

      <div className="screen-only mt-3 d-flex gap-2 justify-content-center">
        <button className="btn btn-primary" type="button" onClick={() => window.print()}>Print igen</button>
        <button className="btn btn-outline-secondary" type="button" onClick={() => navigateTo('/app')}>Tilbage</button>
      </div>
    </main>
  );
}
