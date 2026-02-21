import { formatPrintTimestamp } from '../utils/printTimestamp';
import { printHtmlViaHiddenIframe } from './printHtmlViaHiddenIframe';

export function openPrinterSetupDialog() {
  const printedAt = formatPrintTimestamp();
  const html = `<!doctype html>
<html lang="da">
<head>
  <meta charset="utf-8" />
  <title>Printer opsætning</title>
  <style>
    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 24px; }
    .label { display: grid; gap: 8px; justify-items: center; }
    .title { font-size: 18px; font-weight: 700; letter-spacing: 0.03rem; text-transform: uppercase; color: #334155; }
    .hint { font-size: 14px; color: #334155; text-align: center; }
    .timestamp { font-size: 11px; color: #64748b; }
  </style>
</head>
<body>
  <div class="label">
    <div class="title">Printer opsætning</div>
    <div class="hint">Vælg ønsket printer i dialogen. Browseren husker normalt seneste destination.</div>
    <div class="timestamp">Udskrevet: ${printedAt}</div>
  </div>
  <script>window.print();</script>
</body>
</html>`;

  printHtmlViaHiddenIframe(html);
}
