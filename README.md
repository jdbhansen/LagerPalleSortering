# LagerPalleSortering

Intern Blazor-app til varemodtagelse og palle-styring i lagerdrift.

## Start Her
- Kør app lokalt: `dotnet run`
- Verificer build + tests: `.\scripts\verify.ps1`
- Lav work package: `.\scripts\package-work.ps1`

## Indhold
- [Formål](#formål)
- [Dokumentation](#dokumentation)
- [Nøglefunktioner](#nøglefunktioner)
- [Hurtig start](#hurtig-start)
- [Verificering](#verificering)
- [Tag med på arbejde (portable testpakke)](#tag-med-på-arbejde-portable-testpakke)
- [Eksport og drift endpoints](#eksport-og-drift-endpoints)
- [Konfiguration](#konfiguration)
- [Arkitektur](#arkitektur)
- [Git LFS](#git-lfs)

## Formål
Appen reducerer fejl i palle-placering ved at styre registrering, palleforslag, label-print og flyttebekræftelse med scan.

## Dokumentation
- Brugerguide: [`docs/USER_GUIDE.md`](docs/USER_GUIDE.md)
- Teknisk guide: [`docs/TECHNICAL_GUIDE.md`](docs/TECHNICAL_GUIDE.md)
- Drift/fejlsøgning: [`docs/OPERATIONS.md`](docs/OPERATIONS.md)
- Work package zip i repo: [`work-package/LagerPalleSortering-work.zip`](work-package/LagerPalleSortering-work.zip)

## Nøglefunktioner
- Registrering af `varenummer`, `holdbarhed (YYYYMMDD, gyldig dato)` og `antal kolli`.
- Automatisk pallevalg med guardrails:
- Maks 4 vare+dato-varianter pr. palle.
- Samme stregkode med forskellig holdbarhed må ikke blandes på samme palle.
- Print af pallelabel med Code128 stregkode (`PALLET:P-001`).
- Print af palleindhold med scanbare produkt-stregkoder, holdbarhedsdato og antal.
- Flyttebekræftelse via palle-scan med kolli-tæller (`ConfirmedQuantity/Quantity`).
- Double-scan guard mod utilsigtede hurtige dublet-scans (konfigurerbar).
- Persistens i SQLite (`App_Data/lager.db`).
- Backup (`/backup/db`) og restore direkte i UI.
- Audit-log for kritiske handlinger (registrer, luk, bekræft, undo, clear, backup/restore) gemmes i databasen.
- Eksport til CSV og Excel.
- Drift endpoints: `/health` og `/metrics`.
- Scanner-optimeret inputflow (Enter-baseret).
- Operator-status vises med tydelig alert-styling i toppen af siden.
- "Database restore" er placeret nederst på forsiden for at mindske fejlklik i primært scan-flow.

## Tastaturgenveje
- `Enter` i varenummer: flytter fokus til holdbarhed.
- `Enter` i holdbarhed/antal: registrerer kolli.
- `Enter` i palle-scan/antal at bekræfte: bekræfter flyt.
- `Alt+1`: fokus på varenummer.
- `Alt+2`: fokus på palle-scan.
- `Alt+R`: registrer kolli.
- `Alt+B`: bekræft flyt.
- `Alt+U`: fortryd seneste.
- `Esc`: annuller "ryd database"-advarsel (når den vises).

## Barcode support
- Varekoder: EAN-8, EAN-13, UPC-A.
- UPC-A normaliseres internt til EAN-13.
- Scanner-symbology prefix (fx `]E0`) håndteres.
- Palle-scan er tolerant over for scanner-støj:
  - keyboard-layout mismatch håndteres (`æ/Æ` tolkes som `:` i palle-scan, fx `PALLETæP-001`).
  - `+` normaliseres til `-` (fx `P+001` -> `P-001`).
  - irrelevante tegn ignoreres (fx ekstra bogstaver/symboler før/efter kode).
- Anbefalet drift: match scanner keyboard-country med Windows input-layout for at undgå fejltegn i andre felter.

## Arkitektur
- `Domain/`: kontrakter, regler, barcode-normalisering og barcode-interfaces/standardimplementeringer.
- `Application/`: use-cases og serviceabstraktioner.
- `Infrastructure/`: SQLite repository + migration/query-logik.
- `Components/`: Blazor UI.
- `Services/`: UI-nære services (fx `IBarcodeService` + `BarcodeService`).

## Hurtig start
Forudsætning: .NET SDK 10.

```powershell
dotnet run
```

Appen starter på lokal URL vist i terminalen.

## Verificering
Anbefalet kommando (byg + test, inkl. håndtering af testhost fil-lock):

```powershell
.\scripts\verify.ps1
```

Alternativt:

```powershell
dotnet build LagerPalleSortering.slnx
dotnet test LagerPalleSortering.slnx --no-build
```

Kun sanity/smoke tests:

```powershell
dotnet test LagerPalleSortering.slnx --filter "Category=Sanity"
```

CI i GitHub Actions kører restore + build + test på Windows for `push` til `master` og på `pull_request`.
Derudover kører CI:
- Work-package sync check (hash-match mellem genereret og tracked zip)
- `dotnet format --verify-no-changes`
- Coverage gate (line total >= 65%)
- Playwright UI sanity tests

## Tag med på arbejde (portable testpakke)
Byg en transportabel Windows-pakke (self-contained) med startscript:

```powershell
.\scripts\package-work.ps1
```

Output:
- Mappe: `..\LagerPalleSortering-work-package\app`
- Zip: `..\LagerPalleSortering-work-package\LagerPalleSortering-work.zip`
- Repo-download: [`work-package/LagerPalleSortering-work.zip`](work-package/LagerPalleSortering-work.zip)
- Scriptet synkroniserer automatisk den tracked zip i `work-package/` (Git LFS).

På arbejds-PC:
1. Pak zip-filen ud.
2. Kør `Start-Lager.cmd`.
3. Appen starter på `http://127.0.0.1:5050`.

## Eksport og drift endpoints
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

## Git LFS
- `work-package/*.zip` tracks via Git LFS.
- Ved klon på ny maskine: `git lfs install` og `git lfs pull`.
