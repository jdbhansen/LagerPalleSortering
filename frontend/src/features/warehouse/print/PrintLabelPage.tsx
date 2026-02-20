import { useMemo } from 'react';
import JsBarcode from 'jsbarcode';
import { navigateTo } from '../../../navigation';
import { usePrintOnMount } from '../hooks/usePrintOnMount';
import { toPalletBarcodePayload } from '../utils/palletBarcodePayload';

interface PrintLabelPageProps {
  palletId: string;
}

function createBarcodeSvg(value: string, width = 2, height = 88): string {
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  JsBarcode(svg, value, {
    format: 'CODE128',
    displayValue: false,
    width,
    height,
    margin: 6,
  });
  return svg.outerHTML;
}

export function PrintLabelPage({ palletId }: PrintLabelPageProps) {
  const payload = useMemo(() => toPalletBarcodePayload(palletId), [palletId]);
  const barcodeMarkup = useMemo(() => createBarcodeSvg(payload), [payload]);
  usePrintOnMount();

  return (
    <main className="print-page print-label-page">
      <section className="print-sheet print-label-sheet">
        <h1 className="print-title">PALLE</h1>
        <div className="print-pallet-id">{palletId}</div>
        <div className="print-barcode" dangerouslySetInnerHTML={{ __html: barcodeMarkup }} />
        <div className="print-code">{payload}</div>
        <div className="print-timestamp">Udskrevet: {new Date().toLocaleString('da-DK')}</div>
      </section>

      <div className="screen-only mt-3 d-flex gap-2 justify-content-center">
        <button className="btn btn-primary" type="button" onClick={() => window.print()}>Print igen</button>
        <button className="btn btn-outline-secondary" type="button" onClick={() => navigateTo('/app')}>Tilbage</button>
      </div>
    </main>
  );
}
