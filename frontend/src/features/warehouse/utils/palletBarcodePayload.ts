const palletIdPattern = /^P-\d+$/;
const palletPayloadPattern = /^PALLET:P-\d+$/;

export function toPalletBarcodePayload(palletId: string): string {
  const trimmed = palletId.trim().toUpperCase().replaceAll('+', '-');
  if (!palletIdPattern.test(trimmed)) {
    throw new Error('Ugyldigt palle-id til label. Forventet format: P-001');
  }

  return `PALLET:${trimmed}`;
}

export function isValidPalletBarcodePayload(payload: string): boolean {
  return palletPayloadPattern.test(payload.trim().toUpperCase().replaceAll('+', '-'));
}
