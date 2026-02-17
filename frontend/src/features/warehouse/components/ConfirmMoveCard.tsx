interface ConfirmMoveCardProps {
  scannedPalletCode: string;
  confirmScanCount: number;
  lastSuggestedPalletId: string;
  onScannedPalletCodeChange: (value: string) => void;
  onConfirmScanCountChange: (value: number) => void;
  onSubmit: () => Promise<void>;
  onError: (error: unknown) => void;
}

export function ConfirmMoveCard({
  scannedPalletCode,
  confirmScanCount,
  lastSuggestedPalletId,
  onScannedPalletCodeChange,
  onConfirmScanCountChange,
  onSubmit,
  onError,
}: ConfirmMoveCardProps) {
  const palletCodeInputId = 'confirm-pallet-code';
  const confirmCountInputId = 'confirm-scan-count';

  return (
    <div className="card border-0 shadow-sm h-100">
      <div className="card-body">
        <h2 className="h5 mb-3">Bekræft flytning</h2>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            onSubmit().catch(onError);
          }}
        >
          <div className="mb-3">
            <label className="form-label" htmlFor={palletCodeInputId}>Scannet pallekode</label>
            <input
              id={palletCodeInputId}
              className="form-control"
              value={scannedPalletCode}
              onChange={(event) => onScannedPalletCodeChange(event.target.value)}
              placeholder={lastSuggestedPalletId ? `PALLET:${lastSuggestedPalletId}` : 'PALLET:P-001'}
            />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor={confirmCountInputId}>Antal at bekræfte</label>
            <input
              id={confirmCountInputId}
              className="form-control"
              type="number"
              min={1}
              value={confirmScanCount}
              onChange={(event) => onConfirmScanCountChange(Number(event.target.value) || 1)}
            />
          </div>
          <button className="btn btn-primary" type="submit">Bekræft flyt</button>
        </form>
      </div>
    </div>
  );
}
