# Drift og Fejlsøgning

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering:
Fast commit/push tjekliste er oprettet og linket fra centrale docs
(2026-02-21).

## Start og stop

Start lokalt:

```powershell
dotnet run
```

- UI: `/app`
- Database: PostgreSQL (default) via `Database` i `appsettings*.json`
- Test/e2e kan køre med SQLite override (`Database__Provider=Sqlite`)
- Login styres via `Auth` i `appsettings*.json`

Stop: luk processen i terminalen.

## Print uden OK-dialog (kiosk-printing)

Start app i browser med kiosk-printing:

```powershell
./scripts/start-kiosk-print.ps1
```

Valgfrit:

```powershell
./scripts/start-kiosk-print.ps1 -Browser chrome -AppUrl "http://localhost:5000/app"
```

Driftsnote:

- Browseren skal køres med `--kiosk-printing` for at undgå OK-dialogen på hver udskrift.
- Printer vælges i UI med `Vælg/skift printer` (typisk én gang pr. station).
- Printervalg gemmes i kiosk-browserprofilen (`App_Data/browser-kiosk-profile`).

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

- CI artifact: `work-package` (uploades af workflow)

## Runbook: typiske problemer

### UI/API fejl

- Tjek browser console for `/api/v1/warehouse/*`
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
- `Gendan database`: brug kun valideret backup (`.json` for PostgreSQL)
- Tag backup før restore/rydning
