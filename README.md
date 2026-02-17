# LagerPalleSortering

Intern app til varemodtagelse og palle-styring i lagerdrift.
Stack: React frontend + ASP.NET backend + SQLite.
Sidst opdateret: 2026-02-17.

## Start her
- Kør app lokalt: `dotnet run`
- Build + test: `./scripts/verify.ps1`
- Byg work package: `./scripts/package-work.ps1`

## Indhold
- [Formål](#formål)
- [Dokumentation](#dokumentation)
- [Nøglefunktioner](#nøglefunktioner)
- [Hurtig start](#hurtig-start)
- [Test og kvalitet](#test-og-kvalitet)
- [Work package (arbejds-pc)](#work-package-arbejds-pc)
- [Endpoints](#endpoints)
- [Konfiguration](#konfiguration)
- [Navngivning og kodepraksis](#navngivning-og-kodepraksis)
- [Arkitektur](#arkitektur)
- [Git LFS](#git-lfs)

## Formål
Appen reducerer fejl i palle-placering ved at styre registrering, palleforslag, label-print og flyttebekræftelse med scan.

## Dokumentation
- Brugerguide: [`docs/USER_GUIDE.md`](docs/USER_GUIDE.md)
- Operator-flow (React): [`docs/OPERATOR_FLOW.md`](docs/OPERATOR_FLOW.md)
- Teknisk guide: [`docs/TECHNICAL_GUIDE.md`](docs/TECHNICAL_GUIDE.md)
- Drift/fejlsøgning: [`docs/OPERATIONS.md`](docs/OPERATIONS.md)
- Frontend-noter: [`frontend/README.md`](frontend/README.md)
- Work package zip i repo: [`work-package/LagerPalleSortering-work.zip`](work-package/LagerPalleSortering-work.zip)

## Nøglefunktioner
- Registrering af `varenummer`, `holdbarhed (YYYYMMDD)` og `antal kolli`
- Automatisk pallevalg med guardrails
- Maks 4 vare+dato-varianter pr. palle
- Samme stregkode med forskellig holdbarhed må ikke blandes på samme palle
- Flyttebekræftelse via palle-scan med kolli-tæller
- Backup/restore af database i UI
- Eksport til CSV/Excel
- Health + metrics endpoints
- Simpel scanner-visning til håndscanner-drift
- Datostregkode i React UI (markeret tydeligt som dato/holdbarhed)

## Hurtig start
Forudsætning: .NET SDK 10 og Node.js LTS.

```powershell
dotnet run
```

- Appen starter på lokal URL vist i terminalen.
- React frontend serveres på `/app`.
- Legacy Blazor-side findes på `/legacy`.

### Frontend build (React)

```powershell
cd frontend
npm install
npm run build
cd ..
```

Build-output skrives til `wwwroot/app`.

## Test og kvalitet
Anbefalet:

```powershell
./scripts/verify.ps1
```

Manuelt:

```powershell
dotnet build LagerPalleSortering.slnx
dotnet test LagerPalleSortering.slnx
cd frontend
npm run lint
npm run test
npm run build
cd ..
npm run test:e2e
```

## Work package (arbejds-pc)
Byg transportabel Windows-pakke (self-contained):

```powershell
./scripts/package-work.ps1
```

Output:
- Mappe: `..\LagerPalleSortering-work-package\app`
- Zip: `..\LagerPalleSortering-work-package\LagerPalleSortering-work.zip`
- Tracked zip: `work-package/LagerPalleSortering-work.zip`

På arbejds-pc:
1. Pak zip ud.
2. Kør `Start-Lager.cmd`.
3. Appen starter på `http://127.0.0.1:5050`.

## Endpoints
- API: `/api/warehouse/*`
- CSV: `GET /export/csv`
- Excel: `GET /export/excel`
- DB backup: `GET /backup/db`
- Health: `GET /health`
- Metrics: `GET /metrics`

## Konfiguration
`appsettings*.json`:

```json
"WarehouseRules": {
  "MaxVariantsPerPallet": 4,
  "EnableDuplicateScanGuard": true,
  "DuplicateScanWindowMs": 1200
}
```

## Navngivning og kodepraksis
- API-funktioner bruger verber (`registerWarehouseColli`, `confirmWarehouseMove`)
- Typer bruger substantiv + suffiks (`WarehouseDashboardResponse`, `WarehouseOperationResponse`)
- Komponenter navngives efter ansvar (`RegisterColliCard`, `ConfirmMoveCard`)
- Hooks prefikses med `use` og indeholder sideorkestrering (`useWarehousePage`)
- API-klient i React følger interface-kontrakt (`WarehouseApiClientContract`) for testbarhed
- Kommentarer bruges kun ved ikke-triviel intent, ikke til linje-for-linje forklaring

## Arkitektur
- `frontend/`: React UI (feature-opdelt warehouse-modul)
- `Api/`: Minimal API endpoints + API contracts
- `Domain/`: kontrakter/regler/barcode-domæne
- `Application/`: use-cases og services
- `Infrastructure/`: SQLite repository og migration/query logik
- `Components/`: Blazor print/layout/legacy-sider
- `Services/`: UI-nære services (barcode + scanner interop)

## Git LFS
- `work-package/*.zip` er tracket via Git LFS.
- Ny maskine:

```powershell
git lfs install
git lfs pull
```
