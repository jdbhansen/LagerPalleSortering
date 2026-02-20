export interface WarehouseApiRoutes {
  dashboard: string;
  register: string;
  confirm: string;
  undo: string;
  clear: string;
  restore: string;
  pallet: (palletId: string) => string;
  palletContents: (palletId: string) => string;
  closePallet: (palletId: string) => string;
}

export interface WarehouseHttpClient {
  requestJson<T>(input: RequestInfo, init?: RequestInit): Promise<T>;
}

export class FetchWarehouseHttpClient implements WarehouseHttpClient {
  private readonly fetchImpl: typeof fetch;

  constructor(fetchImpl?: typeof fetch) {
    // Bind default browser fetch to avoid "Illegal invocation" in some runtimes.
    this.fetchImpl = fetchImpl ?? globalThis.fetch.bind(globalThis);
  }

  async requestJson<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
    const response = await this.fetchImpl(input, init);
    // Some backend failures may return empty/non-JSON bodies, so parsing must be defensive.
    const payload = await response.json().catch(() => null);

    if (!response.ok) {
      const message = payload?.message ?? 'Netværksfejl';
      throw new Error(message);
    }

    return payload as T;
  }
}
