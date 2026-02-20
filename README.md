# LagerPalleSortering

Sidst opdateret: 2026-02-20.

LagerPalleSortering er en intern app til varemodtagelse og pallehåndtering.
Stack: ASP.NET (Minimal API) + React (SPA) + SQLite.

## Hurtig start

1. Installer .NET 10 SDK og Node.js 20+.
2. Kør backend:

```powershell
dotnet run
```

3. Åbn appen på `http://localhost:5000/app` (eller den URL der vises i terminalen).

## Daglig udvikling

- Backend build/test:

```powershell
dotnet build
dotnet test
```

- Frontend lint/test/build:

```powershell
npm --prefix frontend run lint
npm --prefix frontend run test -- --run
npm --prefix frontend run build
```

- Samlet verificering:

```powershell
./scripts/verify.ps1
```

## Opret work package

```powershell
./scripts/package-work.ps1
```

Output:
- `../LagerPalleSortering-work-package/app`
- `../LagerPalleSortering-work-package/LagerPalleSortering-work.zip`
- synkroniseret zip i `work-package/LagerPalleSortering-work.zip`

## Funktioner

- To driftsvisninger:
  - `Ny pallesortering` (sekventielt flow: registrer -> bekræft)
  - `Fuld oversigt`
- GS1 parsing (`AI(01)` + `AI(17)`) ved scanning.
- Dato-normalisering (`YYMMDD` -> `YYYYMMDD`, når gyldig).
- Label-print for palle og palleindhold.
- Backup, restore, health og metrics endpoints.

## Arkitektur (kort)

- `Api/`: HTTP endpoints
- `Application/`: use-cases/services
- `Domain/`: regler, normalisering og parsing
- `Infrastructure/`: persistence og database-provider
- `frontend/`: React SPA
- `tests/`: .NET tests

## Dokumentation

- Brugerguide: `docs/USER_GUIDE.md`
- Operatørflow: `docs/OPERATOR_FLOW.md`
- Drift/fejlsøgning: `docs/OPERATIONS.md`
- Teknisk guide: `docs/TECHNICAL_GUIDE.md`
- Migration notes: `docs/MIGRATION_NOTES.md`
- Scanner-validering: `docs/SCANNER_VALIDATION.md`
- Frontend-noter: `frontend/README.md`
