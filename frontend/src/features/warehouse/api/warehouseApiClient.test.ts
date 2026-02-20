import { describe, expect, it, vi } from 'vitest';
import type { WarehouseApiRoutes, WarehouseHttpClient } from './warehouseApiInfrastructure';
import { createWarehouseApiClient } from './warehouseApiClient';

function createRoutes(): WarehouseApiRoutes {
  return {
    dashboard: '/v2/warehouse/dashboard',
    register: '/v2/warehouse/register',
    confirm: '/v2/warehouse/confirm',
    undo: '/v2/warehouse/undo',
    clear: '/v2/warehouse/clear',
    restore: '/v2/warehouse/restore',
    pallet: (palletId) => `/v2/warehouse/pallets/${encodeURIComponent(palletId)}`,
    palletContents: (palletId) => `/v2/warehouse/pallets/${encodeURIComponent(palletId)}/contents`,
    closePallet: (palletId) => `/v2/warehouse/pallets/${encodeURIComponent(palletId)}/close`,
  };
}

describe('warehouseApiClient', () => {
  it('bruger injicerede routes/transport for dashboard', async () => {
    const requestJson = vi.fn().mockResolvedValue({ openPallets: [], entries: [] });
    const httpClient: WarehouseHttpClient = { requestJson };
    const client = createWarehouseApiClient({ routes: createRoutes(), httpClient });

    await client.fetchWarehouseDashboard();

    expect(requestJson).toHaveBeenCalledWith('/v2/warehouse/dashboard');
  });

  it('sender register payload via transport', async () => {
    const requestJson = vi.fn().mockResolvedValue({ type: 'success', message: 'ok' });
    const httpClient: WarehouseHttpClient = { requestJson };
    const client = createWarehouseApiClient({ routes: createRoutes(), httpClient });

    await client.registerWarehouseColli('ITEM-1', '20261231', 2);

    expect(requestJson).toHaveBeenCalledWith('/v2/warehouse/register', expect.objectContaining({
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    }));
  });
});
