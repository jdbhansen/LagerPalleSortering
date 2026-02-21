# LagerPalleSortering

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering:
Fast commit/push tjekliste er oprettet og linket fra centrale docs
(2026-02-21).
Intern løsning til varemodtagelse og pallehåndtering.

- Backend: ASP.NET Minimal API
- Frontend: React (SPA)
- Data: SQLite

## Kom hurtigt i gang

1. Installer:

   - .NET SDK 10
   - Node.js 22+

1. Start app:

```powershell
dotnet run
```

1. Åbn:

   - `http://localhost:5000/app`

## Login

Applikationen kræver login før brug.

Standard (skal ændres med det samme):

- Brugernavn: `admin`
- Adgangskode: `ChangeMe-Now!`

Konfiguration findes i `appsettings.json` under `Auth`.

## Daglig udvikling

Kør disse kommandoer før push:

```powershell
dotnet build
dotnet test
npm --prefix frontend run lint
npm --prefix frontend run test -- --run
npm --prefix frontend run build
```

Hvis du vil køre standard backend-verifikation:

```powershell
./scripts/verify.ps1
```

## Print uden OK-dialog

For at undgå print-OK dialogen på hver udskrift, start browseren i
kiosk-printing mode:

```powershell
./scripts/start-kiosk-print.ps1
```

Valgfrit:

```powershell
./scripts/start-kiosk-print.ps1 -Browser chrome -AppUrl "http://localhost:5000/app"
```

Bemærk:

- Webappen kan ikke selv tvinge printervalg i browseren.
- Vælg printer én gang via `Vælg/skift printer` i UI; kiosk-profilen
  husker normalt valget.

## CI-paritet lokalt

Samme checks som CI (inkl. work package + e2e):

```powershell
dotnet restore LagerPalleSortering.slnx
dotnet build LagerPalleSortering.slnx --configuration Release --no-restore
dotnet format LagerPalleSortering.slnx --verify-no-changes
dotnet test LagerPalleSortering.slnx --configuration Release --no-build
npm ci
npx playwright install --with-deps chromium
npm run test:e2e
```

## Work Package

Generer work package lokalt:

```powershell
./scripts/package-work.ps1
```

Output:

- `../LagerPalleSortering-work-package/app`
- `../LagerPalleSortering-work-package/LagerPalleSortering-work.zip`
- `work-package/` (lokal output-mappe, ikke tracket i git)

## Nøglefunktioner

- `Ny pallesortering`: sekventielt scannerflow (ét aktivt trin ad gangen)
- `Fuld oversigt`: driftsdashboard med historik og værktøjer
- GS1 parsing (`AI(01)` + `AI(17)`)
- Dato-normalisering (`YYMMDD` -> `YYYYMMDD` når gyldig)
- Print af pallelabel, palleindhold og dato-label (med udskriftstidspunkt)
- Backup, restore, health og metrics endpoints

## Repo-struktur

- `Api/`: HTTP endpoints og contracts
- `Application/`: use-cases/services
- `Domain/`: forretningsregler, parsing, normalisering
- `Infrastructure/`: persistence, db-provider, repositories
- `frontend/`: React-klient
- `tests/`: backend tests
- `e2e/`: Playwright tests

## Dokumentation

- [Brugerguide](docs/USER_GUIDE.md): slutbruger-guide
- [Operatørflow](docs/OPERATOR_FLOW.md): operatørflow og beslutningspunkter
- [Drift og Fejlsøgning](docs/OPERATIONS.md): drift, runbook og fejlsøgning
- [Teknisk Guide](docs/TECHNICAL_GUIDE.md): arkitektur og extension points
- [Migration Notes](docs/MIGRATION_NOTES.md): migrations-seams og playbooks
- [Scanner Validation](docs/SCANNER_VALIDATION.md): scanner-validering
- [Branch Policy](docs/BRANCH_POLICY.md): PR-gates og branch-beskyttelse
- [Commit Push Checklist](docs/COMMIT_PUSH_CHECKLIST.md): fast commit/push arbejdsgang
- [Frontend README](frontend/README.md): frontend-udvikling
