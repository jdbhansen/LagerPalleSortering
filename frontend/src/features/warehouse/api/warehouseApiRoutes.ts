import type { WarehouseApiRoutes } from './warehouseApiInfrastructure';

export interface WarehouseApiRouteOptions {
  basePath?: string;
}

export function createWarehouseApiRoutes(options: WarehouseApiRouteOptions = {}): WarehouseApiRoutes {
  const configuredBasePath = options.basePath ?? '/api/v1/warehouse';
  const basePath = configuredBasePath.endsWith('/') ? configuredBasePath.slice(0, -1) : configuredBasePath;

  return {
    dashboard: `${basePath}/dashboard`,
    register: `${basePath}/register`,
    confirm: `${basePath}/confirm`,
    undo: `${basePath}/undo`,
    clear: `${basePath}/clear`,
    restore: `${basePath}/restore`,
    pallet: (palletId: string) => `${basePath}/pallets/${encodeURIComponent(palletId)}`,
    palletContents: (palletId: string) => `${basePath}/pallets/${encodeURIComponent(palletId)}/contents`,
    closePallet: (palletId: string) => `${basePath}/pallets/${encodeURIComponent(palletId)}/close`,
  };
}
