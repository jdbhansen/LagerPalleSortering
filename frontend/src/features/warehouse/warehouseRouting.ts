export interface WarehousePrintRoute {
  type: 'label' | 'contents';
  palletId: string;
  format?: string | null;
}

export function getWarehousePrintRoute(pathname: string, search: string): WarehousePrintRoute | null {
  const labelMatch = pathname.match(/^\/app\/print-label\/([^/]+)$/);
  if (labelMatch) {
    return { type: 'label', palletId: decodeURIComponent(labelMatch[1]) };
  }

  const contentsMatch = pathname.match(/^\/app\/print-pallet-contents\/([^/]+)$/);
  if (contentsMatch) {
    const query = new URLSearchParams(search);
    return {
      type: 'contents',
      palletId: decodeURIComponent(contentsMatch[1]),
      format: query.get('format'),
    };
  }

  return null;
}

export function getPrintLabelPath(palletId: string): string {
  return `/app/print-label/${encodeURIComponent(palletId)}`;
}

export function getPrintPalletContentsPath(palletId: string, format = 'label190x100'): string {
  return `/app/print-pallet-contents/${encodeURIComponent(palletId)}?format=${encodeURIComponent(format)}`;
}
