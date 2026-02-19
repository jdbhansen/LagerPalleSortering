# Teknisk Guide
Sidst opdateret: 2026-02-19.

## Arkitekturoversigt
- `frontend/`
  - React SPA under `/app`
  - modes: `NewPalletSortingPage` og `WarehousePage`
  - print-ruter i samme SPA
- `Api/`
  - `WarehouseApiEndpoints`: lageroperationer
  - `OperationalApiEndpoints`: export/backup/health/metrics
- `Application/`
  - `WarehouseDataService`, `WarehouseExportService`
- `Domain/`
  - barcode-normalisering/parsing + regler
- `Infrastructure/`
  - `SqliteWarehouseRepository` (partial classes)

## Frontend refaktorering (vedligehold)
- `src/features/warehouse/constants.ts`
  - fælles storage keys, defaults og barcode-regex
- `src/features/warehouse/warehouseRouting.ts`
  - parsing og opbygning af print-ruter
- `src/features/warehouse/hooks/useNewPalletSorting.ts`
  - state samlet i form-interface + API client injection via kontrakt
- `src/features/warehouse/hooks/usePrintOnMount.ts`
  - genbrugelig print-sideeffekt
- `src/features/warehouse/api/warehouseApiClient.ts`
  - centraliserede endpoint-stier

## Backend refaktorering (vedligehold)
- `Api/WarehouseOperationTypes.cs`
  - centraliserede operationstyper (`success`, `warning`, `error`)
- `Program.cs`
  - endpoint-mapping flyttet ud i dedikeret endpoint-klasse

## Kontrakter og interfaces
- Frontend: `WarehouseApiClientContract`, view-model interfaces i hooks
- Backend: application abstractions (`IWarehouseDataService`, `IWarehouseRepository`, osv.)

## Teststatus
- Frontend `npm run test` er midlertidigt deaktiveret (no-op) pga. tidligere hæng i Vitest.
- C# tests og e2e kører fortsat som normalt.
