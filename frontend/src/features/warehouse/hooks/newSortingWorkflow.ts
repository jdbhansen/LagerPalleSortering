import { warehouseBarcodeFormats } from '../constants';
import { normalizeExpiryInput } from '../utils/expiryNormalization';
import { parseGs1ProductAndExpiry } from '../utils/gs1Parser';

export type NewPalletSortingStep = 'register' | 'confirm';

export interface RegisterPayloadResult {
  product: string;
  expiry: string;
}

export interface ValidationResult<TValue> {
  success: boolean;
  value?: TValue;
  errorMessage?: string;
}

export function validateRegisterPayload(productNumberRaw: string, expiryRaw: string): ValidationResult<RegisterPayloadResult> {
  const parsedScan = parseGs1ProductAndExpiry(productNumberRaw);
  // Prefer scanner-parsed GTIN when present, fallback to raw text input.
  const product = (parsedScan?.productNumber ?? productNumberRaw).trim();
  const normalizedExpiryInput = normalizeExpiryInput(expiryRaw);
  const manualExpiry = normalizedExpiryInput.trim();
  // Manual expiry has priority when valid; otherwise use parsed GS1 expiry from same scan payload.
  const expiry = warehouseBarcodeFormats.expiryDatePattern.test(manualExpiry)
    ? manualExpiry
    : (parsedScan?.expiryDateRaw ?? '');

  if (product.length === 0) {
    return {
      success: false,
      errorMessage: 'Scan kolli stregkode først.',
    };
  }

  if (!warehouseBarcodeFormats.expiryDatePattern.test(expiry)) {
    return {
      success: false,
      errorMessage: 'Holdbarhed skal være 8 cifre i format YYYYMMDD.',
    };
  }

  return {
    success: true,
    value: {
      product,
      expiry,
    },
  };
}

export function resolvePalletCode(scannedPalletCode: string, suggestedPalletId: string): string {
  // Operators may skip re-scan in step 2; suggested pallet keeps flow moving without data loss.
  const fallbackCode = suggestedPalletId ? `PALLET:${suggestedPalletId}` : '';
  return scannedPalletCode.trim() || fallbackCode;
}
