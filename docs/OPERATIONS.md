# Drift og Fejlsøgning

Sidst opdateret: 2026-02-20.

## Drift: start og stop

Start app lokalt:

```powershell
dotnet run
```

- Primær UI: `/app`
- Datafil: `App_Data/lager.db`

Stop app: luk terminalprocessen.

## Standard driftschecks

1. Health endpoint svarer:

```powershell
curl http://localhost:5000/health
```

2. Metrics endpoint svarer:

```powershell
curl http://localhost:5000/metrics
```

3. Seneste backup kan downloades:

```powershell
curl http://localhost:5000/backup/db
```

## Verificering før release

```powershell
dotnet build -warnaserror
dotnet test
npm --prefix frontend run lint
npm --prefix frontend run test -- --run
npm --prefix frontend run build
```

## Work package

```powershell
./scripts/package-work.ps1
```

Output zip: `work-package/LagerPalleSortering-work.zip`

## Fejlsøgning

### Appen reagerer ikke som forventet

- Tjek browser console for fejl mod `/api/warehouse/*`.
- Tjek `/health` og `/metrics`.
- Kør `./scripts/verify.ps1`.

### Scanner-output er forkert

- Symptomer: `:` bliver `æ`, `-` bliver `+`, uventede prefix (`]E0`, `]C1`).
- Systemet håndterer støj i parseren, men verificer scannerprofil alligevel.
- Se `docs/SCANNER_VALIDATION.md`.

### Holdbarhed bliver afvist

- Gyldige formater: `YYYYMMDD` eller gyldig `YYMMDD`.
- Ugyldige kalenderdatoer afvises.

### Build-fejl pga. fil-lås (`*.dswa.cache.json`)

- Kør build/test sekventielt i stedet for parallelle kommandoer.
- Luk eventuelle hængende `dotnet`/`testhost` processer.

## Kritiske operationer

- `Ryd database`: kræver aktiv bekræftelse.
- `Gendan database`: brug kun valideret backup.
- Tag backup før restore/rydning.
