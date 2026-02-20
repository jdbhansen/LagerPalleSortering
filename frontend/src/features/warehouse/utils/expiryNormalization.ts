function isValidDate(year: number, month: number, day: number): boolean {
  const date = new Date(Date.UTC(year, month - 1, day));
  return (
    date.getUTCFullYear() === year
    && date.getUTCMonth() === month - 1
    && date.getUTCDate() === day
  );
}

export function normalizeExpiryInput(rawValue: string): string {
  const trimmed = rawValue.trim();

  if (/^\d{8}$/.test(trimmed)) {
    const year = Number.parseInt(trimmed.slice(0, 4), 10);
    const month = Number.parseInt(trimmed.slice(4, 6), 10);
    const day = Number.parseInt(trimmed.slice(6, 8), 10);
    return isValidDate(year, month, day) ? trimmed : rawValue;
  }

  if (/^\d{6}$/.test(trimmed)) {
    const year = 2000 + Number.parseInt(trimmed.slice(0, 2), 10);
    const month = Number.parseInt(trimmed.slice(2, 4), 10);
    const day = Number.parseInt(trimmed.slice(4, 6), 10);

    if (isValidDate(year, month, day)) {
      return `${year.toString().padStart(4, '0')}${trimmed.slice(2)}`;
    }
  }

  return rawValue;
}
