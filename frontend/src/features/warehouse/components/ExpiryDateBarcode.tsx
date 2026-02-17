import JsBarcode from 'jsbarcode';
import { useEffect, useRef } from 'react';
import { formatExpiryDateForDisplay } from '../utils/expiryDate';

interface ExpiryDateBarcodeProps {
  expiryDateRaw: string;
}

function isValidExpiryDateBarcodeInput(value: string): boolean {
  return /^\d{8}$/.test(value);
}

export function ExpiryDateBarcode({ expiryDateRaw }: ExpiryDateBarcodeProps) {
  const svgRef = useRef<SVGSVGElement | null>(null);
  const isValid = isValidExpiryDateBarcodeInput(expiryDateRaw);
  const displayDate = formatExpiryDateForDisplay(expiryDateRaw);

  useEffect(() => {
    if (!svgRef.current || !isValid) {
      return;
    }

    // Compact barcode profile makes it read as helper label, not primary product barcode.
    JsBarcode(svgRef.current, expiryDateRaw, {
      format: 'CODE128',
      displayValue: false,
      height: 34,
      width: 1.1,
      margin: 6,
    });
  }, [expiryDateRaw, isValid]);

  function printBarcode() {
    if (!svgRef.current || !isValid) {
      return;
    }

    const printWindow = window.open('', '_blank', 'width=380,height=250');
    if (!printWindow) {
      return;
    }

    const svgMarkup = svgRef.current.outerHTML;
    const html = `<!doctype html>
<html lang="da">
<head>
  <meta charset="utf-8" />
  <title>Datostregkode</title>
  <style>
    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 24px; }
    .label { display: grid; gap: 8px; justify-items: center; }
    .title { font-size: 14px; font-weight: 700; letter-spacing: 0.04rem; text-transform: uppercase; color: #334155; }
    .hint { font-size: 12px; color: #64748b; }
  </style>
</head>
<body>
  <div class="label">
    <div class="title">Dato / Holdbarhed</div>
    ${svgMarkup}
    <div class="hint">${displayDate}</div>
  </div>
  <script>window.print();</script>
</body>
</html>`;

    printWindow.document.open();
    printWindow.document.write(html);
    printWindow.document.close();
  }

  return (
    <div className="border border-info-subtle rounded p-2 bg-info-subtle">
      <div className="d-flex justify-content-between align-items-center gap-2 mb-2">
        <span className="small fw-semibold text-info-emphasis">Datostregkode (holdbarhed)</span>
        <button
          className="btn btn-sm btn-outline-info"
          type="button"
          disabled={!isValid}
          onClick={printBarcode}
        >
          Print dato-label
        </button>
      </div>

      {!isValid ? (
        <div className="small text-muted">Indtast 8 cifre (YYYYMMDD). Denne stregkode er kun til holdbarhedsdato.</div>
      ) : (
        <div className="bg-white border rounded p-2">
          <div className="small text-uppercase fw-semibold text-secondary mb-1">Dato / Holdbarhed</div>
          <div className="d-flex justify-content-center">
            <svg ref={svgRef} />
          </div>
          <div className="small text-secondary text-center mt-1">{displayDate}</div>
        </div>
      )}
    </div>
  );
}
