# Teknisk Guide

## Arkitekturoversigt
- `Domain/`
  - `WarehouseContracts`: delte DTO/records.
  - `WarehouseConstants`: centrale konstanter.
  - `ProductBarcodeParser`: normalisering og checkdigit-logik.
  - `WarehouseBarcode`: pallelabel format/parsing.
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

## Kritiske forretningsregler
1. Åben palle med matchende vare+dato prioriteres.
2. Palle med samme vare men anden holdbarhed afvises.
3. Ny variant må kun tilføjes når palle har under 4 varianter.
4. Flyttebekræftelse er per kolli (`ConfirmedQuantity` stiger med 1 per scan).
5. Fuldt bekræftet når `ConfirmedQuantity >= Quantity`.

## Endpoints
- `GET /export/csv`
- `GET /export/excel`

## Teststrategi
- `WarehouseDataServiceTests`: funktions- og regeltests.
- `SanityTests`: hurtig smoke-verifikation af kritiske flows.
- Fælles fixture i `tests/.../TestInfrastructure`.
