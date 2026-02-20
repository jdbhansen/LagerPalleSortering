# Drift og Fejlsøgning
Sidst opdateret: 2026-02-20.

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
npm --prefix frontend run test
npm --prefix frontend run build
npm run test:e2e
```

## Fejlsøgning
### Appen virker låst efter inaktivitet
- Tjek browser console for netværksfejl til `/api/warehouse/*`.
- Tjek `/health` og `/metrics`.
- Verificer at databasen ikke er låst (`App_Data/lager.db`).
- `Indhold på paller` bruger event-baseret opdatering (ikke konstant polling), så opdatering sker ved relevante handlinger.

### Holdbarhedsdato bliver afvist ved scanning
- Ved GS1/QR med `AI(17)` konverteres dato automatisk til `YYYYMMDD`.
- Ved manuel indtastning konverteres gyldig `YYMMDD` automatisk til `YYYYMMDD`.
- Ugyldige kalenderdatoer normaliseres ikke og bliver afvist af validering.

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
