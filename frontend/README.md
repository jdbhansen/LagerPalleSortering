# Frontend (React)

React-frontend til LagerPalleSortering.
Sidst opdateret: 2026-02-19.

## Struktur
- `src/App.tsx`
  - mode-switch mellem `Ny pallesortering` og `Fuld oversigt`
  - print-route dispatch via `warehouseRouting.ts`
- `src/navigation.ts`
  - intern SPA-navigation (`navigateTo`, `subscribeNavigation`)
- `src/shared/errorMessage.ts`
  - fælles fejltekst-mapping

## Warehouse feature
- `src/features/warehouse/constants.ts`
  - storage keys, defaults og valideringsmønstre
- `src/features/warehouse/warehouseRouting.ts`
  - route parser + route builders for print
- `src/features/warehouse/hooks/useNewPalletSorting.ts`
  - stateful flow-hook med form-interface og API-kontrakt
- `src/features/warehouse/hooks/useWarehousePage.ts`
  - fuld oversigt-state og operationer
- `src/features/warehouse/hooks/usePrintOnMount.ts`
  - genbrugelig auto-print hook
- `src/features/warehouse/components/PalletContentsOverviewCard.tsx`
  - palleindhold, lukning og genprint
- `src/features/warehouse/print/*`
  - dedikerede printvisninger

## Principper
- Ingen nye browserfaner i normal flow
- Sideeffekter i hooks, UI-komponenter holdes så presentational som muligt
- API-klient abstraheres via `WarehouseApiClientContract`
- Undgå magic strings via konstanter og route-builders

## Udvikling

```powershell
cd frontend
npm install
npm run dev
npm run lint
npm run test
npm run build
```

Build-output skrives til `../wwwroot/app`.
