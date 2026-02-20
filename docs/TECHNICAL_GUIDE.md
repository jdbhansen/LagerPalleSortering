# Teknisk Guide

Sidst opdateret: 2026-02-20.

## Formål

Dokumentet beskriver systemets struktur, vigtigste interfaces og hvor ændringer bør placeres.

## Lagdeling

- `Api/`
  - Minimal API endpoints
  - ansvar: HTTP-kontrakter og mapping til services
- `Application/`
  - use-cases (`WarehouseDataService`, `WarehouseExportService`)
  - ansvar: orkestrering af forretningsregler
- `Domain/`
  - normalisering/parsing/forretningsregler
  - ansvar: rene regler uden infrastrukturafhængigheder
- `Infrastructure/`
  - repository og databaseintegration
- `frontend/`
  - React SPA med feature-opdeling

## Vigtige backend interfaces

- `Application/Abstractions/IWarehouseRepository.cs`
- `Application/Abstractions/IWarehouseDataService.cs`
- `Infrastructure/Repositories/IWarehouseDatabaseProvider.cs`

`IWarehouseDatabaseProvider` er migrations-seamet for storage. Skift database ved at levere en ny provider og registrere den i DI.

## Vigtige frontend interfaces

- `frontend/src/features/warehouse/api/warehouseApiClientContract.ts`
- `frontend/src/features/warehouse/api/warehouseApiInfrastructure.ts`
- `frontend/src/features/warehouse/hooks/newSortingStateStore.ts`

Migrations-seams i frontend:
- API transport/routes via `createWarehouseApiClient(...)`
- UI state persistence via `NewSortingStateStore`

## Ny pallesortering: struktur

- `useNewPalletSorting.ts`: hook orchestration
- `newSortingWorkflow.ts`: validering + payload-regler
- `newSortingStateStore.ts`: storage abstrahering

Princip: hold sideeffekter i hook, hold regler i rene utility-moduler.

## Barcode og scanner-kompatibilitet

- Backend:
  - `DefaultProductBarcodeNormalizer`
  - `DefaultPalletBarcodeService`
- Frontend:
  - `utils/palletBarcodePayload.ts`

Tests:
- `tests/LagerPalleSortering.Tests/BarcodeScannerCompatibilityTests.cs`
- `frontend/src/features/warehouse/utils/palletBarcodePayload.test.ts`
- `docs/SCANNER_VALIDATION.md`

## Teststrategi

- Backend: `dotnet test`
- Frontend unit/integration: `npm --prefix frontend run test -- --run`
- Frontend lint/build: `npm --prefix frontend run lint`, `npm --prefix frontend run build`

## Best practices i repoet

- Brug interfaces til integration points.
- Undgå duplikeret valideringslogik; del via fælles utilities.
- Hold endpoints tynde; læg logik i services.
- Tilføj tests når parser/validering ændres.
