# Teknisk Guide
Sidst opdateret: 2026-02-17.

## Hurtig navigation
- [Arkitekturoversigt](#arkitekturoversigt)
- [Datamodel](#datamodel)
- [Forretningsregler](#forretningsregler)
- [API-design](#api-design)
- [Navngivning](#navngivning)
- [Teststrategi](#teststrategi)
- [CI](#ci)
- [Relaterede dokumenter](#relaterede-dokumenter)

## Arkitekturoversigt
- `frontend/`
  - React app med feature-opdeling for warehouse-flow
  - Hook-baseret sideorkestrering (`useWarehousePage`)
- `Api/`
  - Minimal API-endpoints og typed API contracts
- `Domain/`
  - kontrakter, barcode parsing/normalisering, regler
- `Application/`
  - use-case services (`WarehouseDataService`, `WarehouseExportService`)
- `Infrastructure/`
  - `SqliteWarehouseRepository` (partial classes)
- `Components/`
  - Blazor print-sider + layout + legacy-side

## Datamodel
- `Pallets`
- `PalletItems`
- `ScanEntries`
- `AuditEntries`

Se repository-implementering for præcise kolonner og migrationer.

## Forretningsregler
- Maks 4 varianter pr. palle
- Samme stregkode med forskellig dato må ikke blandes på samme palle
- Bekræftelse sker per fysisk kolli
- Duplicate-scan guard kan afvise hurtige gentagne scans
- Palleparser er robust over for scanner-støj (`æ/Æ` som `:` og `+` som `-`)
- Dato vises i grænsefladen som `YYYY-MM-DD`, men lagres/scannes som `YYYYMMDD`

## API-design
Warehouse endpoints under `/api/warehouse`:
- `GET /dashboard`
- `POST /register`
- `POST /confirm`
- `POST /pallets/{palletId}/close`
- `POST /undo`
- `POST /clear`
- `POST /restore`

POST-endpoints er markeret med `DisableAntiforgery()` for scanner-/SPA-flow.
Response-mapping for batch-bekræftelse er centraliseret i endpoint-hjælper for mindre duplikering.

## Navngivning
- C# interfaces: `I`-prefiks (`IWarehouseDataService`)
- C# services: domænenavn + `Service` (`WarehouseDataService`)
- React hooks: `use*`
- React komponenter: PascalCase + ansvar (`OpenPalletsCard`)
- API payloads: `*Request` / `*Response`
- Frontend API-klient abstraheres via `WarehouseApiClientContract`

## Teststrategi
- Unit tests (`tests/LagerPalleSortering.Tests`)
- API integration tests med `WebApplicationFactory`
- Frontend component tests med Vitest + Testing Library
- Browser e2e med Playwright (`e2e/tests`)

## CI
CI validerer:
- restore/build/test
- format check
- coverage gate
- Playwright e2e
- work-package sync

## Relaterede dokumenter
- Projektoversigt: [`README.md`](../README.md)
- Brugerguide: [`docs/USER_GUIDE.md`](USER_GUIDE.md)
- Operator-flow: [`docs/OPERATOR_FLOW.md`](OPERATOR_FLOW.md)
- Drift: [`docs/OPERATIONS.md`](OPERATIONS.md)
