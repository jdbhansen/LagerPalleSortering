export function toErrorMessage(error: unknown, fallback = 'Ukendt fejl'): string {
  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return fallback;
}
