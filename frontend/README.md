# Frontend (React)

Sidst opdateret: 2026-02-20.

## Formål

React SPA for lagerdrift under `/app`.

## Hurtig start

```powershell
cd frontend
npm install
npm run dev
```

## Kommandoer

```powershell
npm run lint
npm run test -- --run
npm run build
```

Build-output skrives til `../wwwroot/app`.

## Struktur

- `src/App.tsx`
  - mode-switch mellem `Ny pallesortering` og `Fuld oversigt`
  - print-route dispatch
- `src/navigation.ts`
  - intern SPA navigation
- `src/shared/errorMessage.ts`
  - central fejltekst-normalisering

### Warehouse feature

- `api/warehouseApiClientContract.ts`
  - API-kontrakt for UI-laget
- `api/warehouseApiInfrastructure.ts`
  - transport-interface (`WarehouseHttpClient`)
- `api/warehouseApiRoutes.ts`
  - route-seam (nem endpoint-migration)
- `api/warehouseApiClient.ts`
  - adapter/factory (`createWarehouseApiClient`)
- `hooks/useNewPalletSorting.ts`
  - sekventielt flow (trin 1 -> trin 2)
- `hooks/newSortingWorkflow.ts`
  - ren validerings- og payloadlogik
- `hooks/newSortingStateStore.ts`
  - storage-interface for flow-state
- `hooks/useWarehousePage.ts`
  - driftsoverblik og operationer
- `utils/gs1Parser.ts`
  - GS1 parsing (`AI(01)`, `AI(17)`)
- `utils/expiryNormalization.ts`
  - dato-normalisering
- `utils/palletBarcodePayload.ts`
  - canonical palle payload + validering

## Best practices i koden

- Hold sideeffekter i hooks, regler i rene utils.
- Genbrug kontrakter/interfaces for integration points.
- Undgå hardcodede routes i komponenter.
- Tilføj tests ved ændring af parsing/validering.

## Testdækning

- Hook-tests for ny sortering og fuld oversigt.
- Workflow-tests for valideringsregler.
- API-klient tests for route/transport adapters.
- Utility-tests for GS1, dato, routing og payload-validering.
