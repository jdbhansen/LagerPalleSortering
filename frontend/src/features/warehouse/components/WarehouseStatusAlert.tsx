import type { WarehouseOperationResponse } from '../models';

interface WarehouseStatusAlertProps {
  status: WarehouseOperationResponse | null;
}

export function WarehouseStatusAlert({ status }: WarehouseStatusAlertProps) {
  if (!status) {
    return null;
  }

  const alertClass =
    status.type === 'success'
      ? 'alert alert-success'
      : status.type === 'warning'
        ? 'alert alert-warning'
        : 'alert alert-danger';

  return (
    <div className={alertClass} role="alert">
      {status.message}
    </div>
  );
}
