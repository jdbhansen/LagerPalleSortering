interface PrinterSetupPanelProps {
  autoPrintEnabled: boolean;
  preferredPrinterName: string;
  onAutoPrintEnabledChange: (value: boolean) => void;
  onPreferredPrinterNameChange: (value: string) => void;
  onOpenPrinterDialog: () => void;
}

export function PrinterSetupPanel({
  autoPrintEnabled,
  preferredPrinterName,
  onAutoPrintEnabledChange,
  onPreferredPrinterNameChange,
  onOpenPrinterDialog,
}: PrinterSetupPanelProps) {
  return (
    <section className="screen-only alert alert-secondary mb-0 print-printer-setup" role="region" aria-label="Printer opsætning">
      <div className="d-flex flex-wrap gap-2 justify-content-center">
        <button className="btn btn-outline-primary" type="button" onClick={onOpenPrinterDialog}>
          Vælg printer
        </button>
      </div>

      <div className="form-check form-switch mt-3 text-start">
        <input
          id="auto-print-toggle"
          className="form-check-input"
          type="checkbox"
          checked={autoPrintEnabled}
          onChange={(event) => onAutoPrintEnabledChange(event.target.checked)}
        />
        <label className="form-check-label" htmlFor="auto-print-toggle">
          Auto-print ved åbning af siden
        </label>
      </div>

      <div className="mt-2 text-start">
        <label htmlFor="printer-name" className="form-label mb-1">Valgt printer (navn)</label>
        <input
          id="printer-name"
          type="text"
          className="form-control"
          placeholder="Fx Zebra ZD421"
          value={preferredPrinterName}
          onChange={(event) => onPreferredPrinterNameChange(event.target.value)}
        />
      </div>

      <p className="small text-muted mb-0 mt-2">
        Browseren kan ikke vælge printer direkte fra webappen. Brug &quot;Vælg printer&quot; til at sætte destinationen i printdialogen.
        For print uden OK-dialog i hver udskrift kræver det kiosk-printing i browseren.
      </p>
    </section>
  );
}
