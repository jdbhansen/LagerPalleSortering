# Frontend (React)

React-frontend til LagerPalleSortering.

## Struktur
- `src/features/warehouse/`
  - `api/warehouseApiClientContract.ts`: API-interfacekontrakt
  - `api/warehouseApiClient.ts`: default API-klientimplementering
  - `hooks/useWarehousePage.ts`: side-logik/state
  - `components/*`: præsentationskomponenter
  - `WarehousePage.tsx`: side-komposition
- `src/App.tsx`: tynd entry-komponent

## Udvikling

```powershell
cd frontend
npm install
npm run dev
```

Krav: backend kører på `http://localhost:5050` for Vite-proxy.

## Kvalitet

```powershell
npm run lint
npm run test
npm run build
```

## UI-tests
- Unit/integration tests ligger i `src/features/warehouse/WarehousePage.test.tsx`
- Test setup ligger i `src/test/setupTests.ts`
- Kør watch-mode med `npm run test:watch`

## Navngivningsstandard
- Komponenter: PascalCase + ansvar (`ConfirmMoveCard`)
- Hooks: `use*` (`useWarehousePage`)
- API-funktioner: verber (`fetchWarehouseDashboard`, `registerWarehouseColli`)
- Typer: `*Response`, `*Entry`, `*Pallet`

## Build til .NET app

```powershell
cd frontend
npm run build
```

Build-output skrives til `wwwroot/app` og serveres på `/app`.
