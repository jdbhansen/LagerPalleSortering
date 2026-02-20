const gs1GroupSeparator = '\u001d';
const scannerPrefixPattern = /^\][A-Za-z0-9]{2}/;

export interface ParsedGs1Scan {
  productNumber: string | null;
  expiryDateRaw: string | null;
}

function stripScannerPrefix(rawValue: string): string {
  return rawValue.replace(scannerPrefixPattern, '');
}

function toExpiryDateRaw(yymmdd: string): string | null {
  if (!/^\d{6}$/.test(yymmdd)) {
    return null;
  }

  const yy = Number.parseInt(yymmdd.slice(0, 2), 10);
  const mm = Number.parseInt(yymmdd.slice(2, 4), 10);
  const dd = Number.parseInt(yymmdd.slice(4, 6), 10);
  const year = 2000 + yy;
  const date = new Date(Date.UTC(year, mm - 1, dd));

  if (
    date.getUTCFullYear() !== year
    || date.getUTCMonth() !== mm - 1
    || date.getUTCDate() !== dd
  ) {
    return null;
  }

  return `${year.toString().padStart(4, '0')}${yymmdd.slice(2)}`;
}

function parseParenthesized(payload: string): ParsedGs1Scan | null {
  const matches = payload.matchAll(/\((\d{2})\)([^()]*)/g);
  let productNumber: string | null = null;
  let expiryDateRaw: string | null = null;

  for (const match of matches) {
    const ai = match[1];
    const value = (match[2] ?? '').split(gs1GroupSeparator)[0].trim();
    if (ai === '01' && /^\d{14}$/.test(value)) {
      productNumber = value;
    }
    if (ai === '17') {
      expiryDateRaw = toExpiryDateRaw(value);
    }
  }

  if (!productNumber && !expiryDateRaw) {
    return null;
  }

  return { productNumber, expiryDateRaw };
}

function parseCompact(payload: string): ParsedGs1Scan | null {
  let index = 0;
  let productNumber: string | null = null;
  let expiryDateRaw: string | null = null;

  while (index < payload.length) {
    const ai = payload.slice(index, index + 2);

    if (ai === '01') {
      const value = payload.slice(index + 2, index + 16);
      if (/^\d{14}$/.test(value)) {
        productNumber = value;
        index += 16;
        continue;
      }
    }

    if (ai === '17') {
      const value = payload.slice(index + 2, index + 8);
      expiryDateRaw = toExpiryDateRaw(value);
      index += 8;
      continue;
    }

    // Variable-length AI; jump to the next GS separator if present.
    if (ai === '10') {
      const nextSeparator = payload.indexOf(gs1GroupSeparator, index + 2);
      if (nextSeparator < 0) {
        break;
      }

      index = nextSeparator + 1;
      continue;
    }

    index += 1;
  }

  if (!productNumber && !expiryDateRaw) {
    return null;
  }

  return { productNumber, expiryDateRaw };
}

export function parseGs1ProductAndExpiry(rawValue: string): ParsedGs1Scan | null {
  const trimmed = stripScannerPrefix(rawValue.trim());
  if (trimmed.length === 0) {
    return null;
  }

  const parenthesized = parseParenthesized(trimmed);
  if (parenthesized) {
    return parenthesized;
  }

  return parseCompact(trimmed);
}
