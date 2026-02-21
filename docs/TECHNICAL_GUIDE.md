# Teknisk Guide

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering: Frontend test setup stabiliseret (fjernet direkte `vitest` import i setup) (2026-02-21).



## Formål

Overblik over arkitektur, contracts og de steder du udvider systemet sikkert.

## Arkitektur

- `Api/`: HTTP-kontrakter og endpoint mapping
- `Application/`: use-cases og orkestrering
- `Domain/`: rene forretningsregler/parsing/normalisering
- `Infrastructure/`: databinding, repositories, providers
- `frontend/`: React feature-moduler

## Backend contracts (primære seams)

- `Application/Abstractions/IWarehouseRepository.cs`
- `Application/Abstractions/IWarehouseDataService.cs`
- `Infrastructure/Repositories/IWarehouseDatabaseProvider.cs`

Storage-migration sker via `IWarehouseDatabaseProvider` + DI-registrering i `Program.cs`.

## Frontend contracts (primære seams)

- `frontend/src/features/warehouse/api/warehouseApiClientContract.ts`
- `frontend/src/features/warehouse/api/warehouseApiInfrastructure.ts`
- `frontend/src/features/warehouse/hooks/newSortingStateStore.ts`

Frontend-migration sker via:
- `createWarehouseApiClient(...)` for transport/routes
- `NewSortingStateStore` for UI-persistens

## Ny pallesortering: ansvar per modul

- `useNewPalletSorting.ts`: sideeffekter og flow-orkestrering
- `newSortingWorkflow.ts`: ren valideringslogik
- `newSortingStateStore.ts`: sessions/state persistence

Regel: sideeffekter i hooks, regler i utils.

## Barcode- og scannerlogik

- Backend:
  - `DefaultProductBarcodeNormalizer`
  - `DefaultPalletBarcodeService`
- Frontend:
  - `utils/palletBarcodePayload.ts`
  - `utils/printTimestamp.ts`

Relaterede tests:
- `tests/LagerPalleSortering.Tests/BarcodeScannerCompatibilityTests.cs`
- `frontend/src/features/warehouse/utils/palletBarcodePayload.test.ts`
- `frontend/src/features/warehouse/utils/printTimestamp.test.ts`

## Teststrategi

- Backend: `dotnet test`
- Frontend tests: `npm --prefix frontend run test -- --run`
- Frontend lint/build: `npm --prefix frontend run lint` og `npm --prefix frontend run build`
- E2E: `npm run test:e2e`

## Kodeprincipper (best practices)

- Hold endpoints tynde; business-logik i services.
- Brug interfaces på integrationspunkter.
- Del validering i fælles utilities.
- Tilføj tests ved parser/valideringsændringer.
- Hold migrations-seams stabile for nem fremtidig udskiftning.
