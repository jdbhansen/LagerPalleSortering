# Drift og Fejlsøgning

Sidst opdateret: 2026-02-21.

## Start og stop

Start lokalt:

```powershell
dotnet run
```

- UI: `/app`
- Datafil: `App_Data/lager.db`

Stop: luk processen i terminalen.

## Hurtig driftscheck

```powershell
curl http://localhost:5000/health
curl http://localhost:5000/metrics
curl http://localhost:5000/backup/db
```

## Release-checkliste

```powershell
dotnet build -warnaserror
dotnet test
npm --prefix frontend run lint
npm --prefix frontend run test -- --run
npm --prefix frontend run build
npm run test:e2e
```

## Work package

```powershell
./scripts/package-work.ps1
```

Tracked artifact:
- `work-package/LagerPalleSortering-work.zip`

## Runbook: typiske problemer

### UI/API fejl

- Tjek browser console for `/api/warehouse/*`
- Verificer `health` og `metrics`
- Kør `./scripts/verify.ps1`

### Scanner giver forkerte tegn

- Symptomer: `:` -> `æ`, `-` -> `+`, prefix som `]E0`/`]C1`
- Parser håndterer en del støj, men scannerprofil skal valideres
- Se `docs/SCANNER_VALIDATION.md`

### Holdbarhed afvises

- Tilladt: `YYYYMMDD` eller gyldig `YYMMDD`
- Ugyldige kalenderdatoer afvises med vilje

### Build/test låser filer

- Kør build/test sekventielt
- Stop hængende `dotnet`/`testhost` processer

## Kritiske handlinger

- `Ryd database`: destruktiv handling, kræver eksplicit bekræftelse
- `Gendan database`: brug kun valideret backup
- Tag backup før restore/rydning
