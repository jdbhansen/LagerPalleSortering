import { ExpiryDateBarcode } from './ExpiryDateBarcode';

interface RegisterColliCardProps {
  productNumber: string;
  expiryDateRaw: string;
  quantity: number;
  onProductNumberChange: (value: string) => void;
  onExpiryDateChange: (value: string) => void;
  onQuantityChange: (value: number) => void;
  onSubmit: () => Promise<void>;
  onError: (error: unknown) => void;
}

export function RegisterColliCard({
  productNumber,
  expiryDateRaw,
  quantity,
  onProductNumberChange,
  onExpiryDateChange,
  onQuantityChange,
  onSubmit,
  onError,
}: RegisterColliCardProps) {
  const productInputId = 'register-product-number';
  const expiryInputId = 'register-expiry-date';
  const quantityInputId = 'register-quantity';

  return (
    <div className="card border-0 shadow-sm h-100">
      <div className="card-body">
        <h2 className="h5 mb-3">Registrer kolli</h2>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            onSubmit().catch(onError);
          }}
        >
          <div className="mb-3">
            <label className="form-label" htmlFor={productInputId}>Varenummer</label>
            <input
              id={productInputId}
              className="form-control"
              value={productNumber}
              onChange={(event) => onProductNumberChange(event.target.value)}
              required
            />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor={expiryInputId}>Holdbarhed (YYYYMMDD)</label>
            <input
              id={expiryInputId}
              className="form-control"
              value={expiryDateRaw}
              onChange={(event) => onExpiryDateChange(event.target.value)}
            />
          </div>

          <div className="mb-3">
            <ExpiryDateBarcode expiryDateRaw={expiryDateRaw} />
          </div>

          <div className="mb-3">
            <label className="form-label" htmlFor={quantityInputId}>Antal kolli</label>
            <input
              id={quantityInputId}
              className="form-control"
              type="number"
              min={1}
              value={quantity}
              onChange={(event) => onQuantityChange(Number(event.target.value) || 1)}
            />
          </div>
          <button className="btn btn-primary" type="submit">Registrer kolli</button>
        </form>
      </div>
    </div>
  );
}
