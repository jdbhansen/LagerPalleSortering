# Teknisk Guide

## Arkitekturoversigt
- `Domain/`
  - `WarehouseContracts`: delte DTO/records.
  - `WarehouseConstants`: centrale konstanter.
  - Interfaces:
    - `IProductBarcodeNormalizer`
    - `IPalletBarcodeService`
  - `ProductBarcodeParser`: normalisering og checkdigit-logik.
  - `WarehouseBarcode`: pallelabel format/parsing med robust scan-sanitization.
  - Standardimplementeringer:
    - `DefaultProductBarcodeNormalizer`
    - `DefaultPalletBarcodeService`
- `Application/`
  - `WarehouseDataService`: forretningsflow (registrering, bekræftelse, undo).
  - `WarehouseExportService`: CSV/Excel eksport.
  - Abstractions i `Application/Abstractions`.
- `Infrastructure/`
  - `SqliteWarehouseRepository` (partials):
    - `Schema`: schema + migration.
    - `Pallets`: palle- og palleitem-query/commands.
    - `ScanEntries`: scanentry- og bekræftelsesquery/commands.
    - `Common`: mapping/helpers.
- `Components/`
  - Blazor UI (`Home`, `PrintLabel`, layout).

## Datamodel (SQLite)
- `Pallets`
  - `PalletId`, `GroupKey`, `ProductNumber`, `ExpiryDate`, `TotalQuantity`, `IsClosed`, `CreatedAt`
- `PalletItems`
  - `PalletId`, `ProductNumber`, `ExpiryDate`, `Quantity`
  - Unik: `(PalletId, ProductNumber, ExpiryDate)`
- `ScanEntries`
  - `Timestamp`, `ProductNumber`, `ExpiryDate`, `Quantity`, `PalletId`, `CreatedNewPallet`,
    `ConfirmedQuantity`, `ConfirmedMoved`, `ConfirmedAt`
- `AuditEntries`
  - `Timestamp`, `Action`, `Details`, `MachineName`

## Kritiske forretningsregler
1. Åben palle med matchende vare+dato prioriteres.
2. Palle med samme vare men anden holdbarhed afvises.
3. Ny variant må kun tilføjes når palle har under 4 varianter.
4. Flyttebekræftelse er per kolli (`ConfirmedQuantity` stiger med 1 per scan).
5. Fuldt bekræftet når `ConfirmedQuantity >= Quantity`.
6. Palle-scan parser udtrækker kun `P-<digits>` og ignorerer øvrig scanner-støj.
7. Duplicate-scan guard kan afvise hurtige dublet-scans (konfigurerbar via `WarehouseRules`).

## Endpoints
- `GET /export/csv`
- `GET /export/excel`
- `GET /backup/db`
- `GET /health`
- `GET /metrics`

## Teststrategi
- `WarehouseDataServiceTests`: funktions- og regeltests.
- `SanityTests`: hurtig smoke-verifikation af kritiske flows.
- `WarehouseBarcodeTests`: parser-/normaliseringstests for palle-scan.
- `ProductBarcodeParserTests`: parser-/normaliseringstests for varestregkoder.
- `e2e/tests/app-health.spec.ts`: UI/endpoint sanity i Playwright.
- `e2e/tests/ui-sanity.spec.ts`: stabile UI sanity-checks af centrale controls/sektioner.
- Fælles fixture i `tests/.../TestInfrastructure`.

## CI
- Workflow: `.github/workflows/ci.yml`
- Kører på `windows-latest`:
  - `dotnet restore LagerPalleSortering.slnx`
  - `dotnet build LagerPalleSortering.slnx --configuration Release --no-restore`
  - work-package sync check (`scripts/package-work.ps1` + SHA256 hash-match)
  - `dotnet format LagerPalleSortering.slnx --verify-no-changes`
  - `dotnet test ...` med coverage (`cobertura`) og line-threshold `>= 65%`
  - `npm ci`
  - `npx playwright install --with-deps chromium`
  - `npm run test:e2e`

## Migration note
- `WarehouseDataService` afhænger nu af barcode-interfaces i stedet for statiske helpers.
- Ved scanner- eller barcode-migration kan du registrere nye implementeringer i `Program.cs` uden at ændre service-flow.
- `WarehouseRules` i `appsettings` styrer centrale guardrails (max varianter, duplicate-scan vindue).
