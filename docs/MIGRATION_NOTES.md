# Migration Notes

Sidst opdateret: 2026-02-21.

## Formål

Guide til kontrolleret migration af API, transport og database uden at bryde brugerflow.

## Eksisterende migrations-seams

### Frontend

- API-factory: `createWarehouseApiClient(...)`
- Transport seam: `WarehouseHttpClient`
- Route seam: `WarehouseApiRoutes`
- UI-state seam: `NewSortingStateStore`

### Backend

- Storage seam: `IWarehouseDatabaseProvider`
- Default implementation: `SqliteWarehouseDatabaseProvider`

## Migrerings-playbooks

### 1. Ny API-version / gateway

1. Implementér nye routes i `createWarehouseApiRoutes(...)` eller custom route map.
2. Bevar `WarehouseApiClientContract`.
3. Verificér flows via frontend tests + e2e.

### 2. Ny frontend transport (retry/auth/proxy)

1. Implementér ny `WarehouseHttpClient`.
2. Injectér i `createWarehouseApiClient({ httpClient })`.
3. Kør API-klient tests og e2e.

### 3. Ny backend database

1. Implementér ny `IWarehouseDatabaseProvider`.
2. Registrér provider i DI (`Program.cs`).
3. Hold `IWarehouseRepository` kontrakten stabil.
4. Kør backend tests + scanner-kompatibilitetstests.

## Gate-checkliste før merge

1. Contracts er bagudkompatible.
2. Nye adapters/providers er dækket af tests.
3. Full verify er grøn:
- `dotnet build -warnaserror`
- `dotnet test`
- `npm --prefix frontend run lint`
- `npm --prefix frontend run test -- --run`
- `npm --prefix frontend run build`
- `npm run test:e2e`
4. Scanner-flow er manuelt valideret jf. `docs/SCANNER_VALIDATION.md`.
