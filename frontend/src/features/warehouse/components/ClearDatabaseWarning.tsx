interface ClearDatabaseWarningProps {
  visible: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ClearDatabaseWarning({ visible, onConfirm, onCancel }: ClearDatabaseWarningProps) {
  if (!visible) {
    return null;
  }

  return (
    <div className="alert alert-danger d-flex flex-wrap align-items-center gap-2" role="alert">
      <strong>Advarsel:</strong> Dette sletter alle paller og registreringer permanent.
      <button className="btn btn-sm btn-danger" type="button" onClick={onConfirm}>
        Ja, ryd database
      </button>
      <button className="btn btn-sm btn-outline-secondary" type="button" onClick={onCancel}>
        Annuller
      </button>
    </div>
  );
}
