import { describe, expect, it } from 'vitest';
import { createWarehouseApiRoutes } from './warehouseApiRoutes';

describe('warehouseApiRoutes', () => {
  it('bruger versioneret basePath som default', () => {
    const routes = createWarehouseApiRoutes();
    expect(routes.dashboard).toBe('/api/v1/warehouse/dashboard');
    expect(routes.register).toBe('/api/v1/warehouse/register');
  });

  it('normaliserer trailing slash i custom basePath', () => {
    const routes = createWarehouseApiRoutes({ basePath: '/api/custom/' });
    expect(routes.dashboard).toBe('/api/custom/dashboard');
    expect(routes.pallet('P-001')).toBe('/api/custom/pallets/P-001');
  });
});
