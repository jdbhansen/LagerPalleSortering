# Teknisk Guide
Sidst opdateret: 2026-02-20.

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
- `src/features/warehouse/components/PalletContentsOverviewCard.tsx`
  - opdaterer indhold via `refreshToken` fra relevante brugerhandlinger
- `src/features/warehouse/utils/gs1Parser.ts`
  - parser GS1 payloads (både parenthesized og compact) for `AI(01)` + `AI(17)`
- `src/features/warehouse/utils/expiryNormalization.ts`
  - normaliserer dato-input (`YYMMDD` -> `YYYYMMDD`) når datoen er gyldig
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
- Frontend tests kører med Vitest i `forks`-pool med `maxWorkers=1` og `fileParallelism=false` for stabil kørsel i CI/lokalt.
- Testsuite fokuserer på stabile enhedstests (routing, formattering, App-view routing/state).
- C# tests og e2e kører fortsat som normalt.
