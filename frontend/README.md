# Frontend (React)

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering: `.gitignore` er opdateret (2026-02-21).


Frontend er en SPA under `/app` til scannerdrevet lagerflow.

- [Til hoved-README](../README.md)

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

Build output lander i `../wwwroot/app`.

## Arkitektur i kort form

- `src/App.tsx`: route entry + mode switch
- `src/navigation.ts`: intern navigation
- `src/shared/errorMessage.ts`: fælles fejl-normalisering

### Warehouse feature

- `api/warehouseApiClientContract.ts`: kontrakt for UI -> API
- `api/warehouseApiInfrastructure.ts`: transport interface
- `api/warehouseApiRoutes.ts`: route factory/seam
- `api/warehouseApiClient.ts`: adapter/factory
- `hooks/useNewPalletSorting.ts`: sekventielt flow
- `hooks/newSortingWorkflow.ts`: ren valideringslogik
- `hooks/newSortingStateStore.ts`: persistence seam for UI-state
- `hooks/useWarehousePage.ts`: fuld oversigt use-cases
- `utils/gs1Parser.ts`: GS1 parsing
- `utils/expiryNormalization.ts`: dato-normalisering
- `utils/palletBarcodePayload.ts`: pallepayload normalisering/validering
- `utils/printTimestamp.ts`: formattering af udskriftstidspunkt

## Kodeprincipper

- Hold sideeffekter i hooks.
- Hold regler og parsing i rene utils.
- Hold API-kontrakt stabil; udskift implementation via interfaces/factory.
- Undgå hardcodede endpoints i komponenter.
- Test altid parser/validering ved ændringer.

## Testfokus

- Hook-tests for flows og edge-cases
- Workflow-tests for valideringsregler
- API-klient tests for route/transport seams
- Utility-tests for GS1, dato, routing, payload og print-tidspunkt
