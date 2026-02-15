# Teknisk Guide

## Arkitektur
- `Domain/`: kontrakter, konstanter og barcode-parser.
- `Application/`: use-cases og service-abstraktioner.
- `Infrastructure/`: SQLite repository og SQL.
- `Components/`: Blazor UI.

## Centrale services
- `IWarehouseDataService` / `WarehouseDataService`
  Ansvar: registrering, validering, pallelukning, fortryd, flyttebekræftelse.
- `IWarehouseExportService` / `WarehouseExportService`
  Ansvar: CSV/Excel eksport.
- `IWarehouseRepository` / `SqliteWarehouseRepository`
  Ansvar: al persistence og query-logik.
- `BarcodeService`
  Ansvar: generere Code128 SVG til printlabel.

## Datamodel (SQLite)
- `Pallets`
  Kolonner: `PalletId`, `GroupKey`, `ProductNumber`, `ExpiryDate`, `TotalQuantity`, `IsClosed`, `CreatedAt`
- `PalletItems`
  Kolonner: `PalletId`, `ProductNumber`, `ExpiryDate`, `Quantity`
  Constraint: unik kombination af palle + vare + dato.
- `ScanEntries`
  Kolonner: registreringshistorik inkl. `ConfirmedMoved`, `ConfirmedAt`.

## Pallevalgsregler
1. Åbne paller evalueres.
2. Paller med samme stregkode men anden holdbarhed udelukkes.
3. Hvis vare+dato findes på palle, genbruges den palle.
4. Hvis ikke, må palle kun vælges hvis den har færre end 4 varianter.
5. Ellers oprettes ny palle.

## Stregkode-normalisering
- `ProductBarcodeParser.Normalize` håndterer:
  - EAN-8
  - EAN-13
  - UPC-A -> EAN-13 (foran med `0`)
  - scanner symbology prefix (`]E0`)
- `WarehouseBarcode` håndterer palleformat:
  - Oprettelse: `PALLET:P-001`
  - Parsing: `PALLET:*` (samt bagudkompatibel `P-*`)

## Endpoints
- `GET /export/csv`
- `GET /export/excel`

## Testpakker
- Fuld suite: `dotnet test LagerPalleSortering.slnx`
- Sanity/smoke: `dotnet test LagerPalleSortering.slnx --filter "Category=Sanity"`
