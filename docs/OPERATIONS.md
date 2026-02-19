# Drift og Fejlsøgning
Sidst opdateret: 2026-02-19.

## Drift
Start app:

```powershell
dotnet run
```

- Primær UI: `/app`
- Data: `App_Data/lager.db`

## Kvalitetskørsel

```powershell
./scripts/verify.ps1
npm --prefix frontend run lint
npm --prefix frontend run build
npm run test:e2e
```

Bemærk: `npm --prefix frontend run test` er deaktiveret (no-op), indtil Vitest er genaktiveret.

## Fejlsøgning
### Appen virker låst efter inaktivitet
- Tjek browser console for netværksfejl til `/api/warehouse/*`.
- Tjek `/health` og `/metrics`.
- Verificer at databasen ikke er låst (`App_Data/lager.db`).

### Print åbner ny fane
- Kør hard refresh (`Ctrl+F5`) for at rydde cache.
- Bekræft at nyeste frontend build ligger i `wwwroot/app/assets`.

### Build/test hænger
- Luk hængende `node`, `vitest`, `dotnet` processer.
- Kør derefter kun nødvendige kommandoer én ad gangen.

## Endpoint-oversigt
- Drift: `GET /health`, `GET /metrics`
- Dataeksport: `GET /export/csv`, `GET /export/excel`, `GET /backup/db`
- Lageroperationer: `/api/warehouse/*`
