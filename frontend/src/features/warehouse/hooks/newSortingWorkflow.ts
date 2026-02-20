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
  const product = (parsedScan?.productNumber ?? productNumberRaw).trim();
  const normalizedExpiryInput = normalizeExpiryInput(expiryRaw);
  const manualExpiry = normalizedExpiryInput.trim();
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
  const fallbackCode = suggestedPalletId ? `PALLET:${suggestedPalletId}` : '';
  return scannedPalletCode.trim() || fallbackCode;
}
