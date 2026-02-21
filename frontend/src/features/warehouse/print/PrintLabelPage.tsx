import { useMemo } from 'react';
import JsBarcode from 'jsbarcode';
import { navigateTo } from '../../../navigation';
import { usePrintOnMount } from '../hooks/usePrintOnMount';
import { usePrintPreferences } from '../hooks/usePrintPreferences';
import { toPalletBarcodePayload } from '../utils/palletBarcodePayload';
import { formatPrintTimestamp } from '../utils/printTimestamp';
import { PrinterSetupPanel } from './PrinterSetupPanel';
import { openPrinterSetupDialog } from './openPrinterSetupDialog';

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
  const printedAt = useMemo(() => formatPrintTimestamp(), []);
  const {
    autoPrintEnabled,
    setAutoPrintEnabled,
    preferredPrinterName,
    setPreferredPrinterName,
  } = usePrintPreferences();
  usePrintOnMount(autoPrintEnabled);

  return (
    <main className="print-page print-label-page">
      <section className="print-sheet print-label-sheet">
        <h1 className="print-title">PALLE</h1>
        <div className="print-pallet-id">{palletId}</div>
        <div className="print-barcode" dangerouslySetInnerHTML={{ __html: barcodeMarkup }} />
        <div className="print-code">{payload}</div>
        <div className="print-timestamp">Udskrevet: {printedAt}</div>
      </section>

      <div className="screen-only mt-3 w-100 print-setup-wrap">
        <PrinterSetupPanel
          autoPrintEnabled={autoPrintEnabled}
          onAutoPrintEnabledChange={setAutoPrintEnabled}
          preferredPrinterName={preferredPrinterName}
          onPreferredPrinterNameChange={setPreferredPrinterName}
          onOpenPrinterDialog={openPrinterSetupDialog}
        />
      </div>

      <div className="screen-only mt-3 d-flex gap-2 justify-content-center">
        <button className="btn btn-primary" type="button" onClick={() => window.print()}>Print igen</button>
        <button className="btn btn-outline-secondary" type="button" onClick={() => navigateTo('/app')}>Tilbage</button>
      </div>
    </main>
  );
}
