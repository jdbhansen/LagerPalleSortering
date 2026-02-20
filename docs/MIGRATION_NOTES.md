# Migration Notes

Sidst opdateret: 2026-02-20.

## Formål

Dette dokument beskriver migrations-seams i løsningen, så skift af backend/frontend-integration kan ske kontrolleret.

## Gennemførte migrationsforberedelser

### Frontend

- API-klient er adapter-baseret:
  - `createWarehouseApiClient(...)`
  - transport seam: `WarehouseHttpClient`
  - route seam: `WarehouseApiRoutes`
- Ny sortering har state seam:
  - `NewSortingStateStore`

### Backend

- Persistence seam for database er indført:
  - `IWarehouseDatabaseProvider`
  - default: `SqliteWarehouseDatabaseProvider`

## Hvordan migrerer vi

### 1. Ny API-version eller ny gateway

- Implementér nye routes via `createWarehouseApiRoutes({ basePath })`.
- Eller injectér custom `WarehouseApiRoutes` direkte i `createWarehouseApiClient(...)`.
- Hooks behøver ikke ændres, så længe `WarehouseApiClientContract` bevares.

### 2. Ny frontend transport (retry, auth, proxy)

- Implementér ny `WarehouseHttpClient`.
- Injectér i `createWarehouseApiClient({ httpClient })`.

### 3. Ny backend database

- Implementér ny `IWarehouseDatabaseProvider`.
- Registrér provider i DI (`Program.cs`).
- Hold `IWarehouseRepository` kontrakten stabil for applikationslaget.

## Checkliste ved migration

1. Hold contracts bagudkompatible (`WarehouseApiClientContract`, `IWarehouseRepository`).
2. Tilføj tests for nye adapters/providers.
3. Kør fuld verifikation:
   - `dotnet build -warnaserror`
   - `dotnet test`
   - `npm --prefix frontend run lint`
   - `npm --prefix frontend run test -- --run`
   - `npm --prefix frontend run build`
4. Verificer barcode/scanner flow manuelt jf. `docs/SCANNER_VALIDATION.md`.
