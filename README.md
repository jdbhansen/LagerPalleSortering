# LagerPalleSortering

Intern Blazor-app til varemodtagelse og palle-styring i lagerdrift.

## Formål
Appen reducerer fejl i palle-placering ved at styre registrering, palleforslag, label-print og flyttebekræftelse med scan.

## Nøglefunktioner
- Registrering af `varenummer`, `holdbarhed (YYYYMMDD)` og `antal kolli`.
- Automatisk pallevalg med guardrails:
  - maks 4 vare+dato-varianter pr. palle.
  - samme stregkode med forskellig holdbarhed må ikke blandes på samme palle.
- Print af pallelabel med Code128 stregkode (`PALLET:P-001`).
- Flyttebekræftelse via palle-scan med kolli-tæller (`ConfirmedQuantity/Quantity`).
- Persistens i SQLite (`App_Data/lager.db`).
- Eksport til CSV og Excel.
- Scanner-optimeret inputflow (Enter-baseret).

## Barcode support
- Varekoder: EAN-8, EAN-13, UPC-A.
- UPC-A normaliseres internt til EAN-13.
- Scanner-symbology prefix (fx `]E0`) håndteres.

## Arkitektur
- `Domain/`: kontrakter, regler og barcode-normalisering.
- `Application/`: use-cases og serviceabstraktioner.
- `Infrastructure/`: SQLite repository + migration/query-logik.
- `Components/`: Blazor UI.

## Hurtig start
Forudsætning: .NET SDK 10.

```powershell
dotnet run
```

Appen starter på lokal URL vist i terminalen.

## Tag med på arbejde (portable testpakke)
Byg en transportabel Windows-pakke (self-contained) med startscript:

```powershell
.\scripts\package-work.ps1
```

Output:
- Mappe: `..\LagerPalleSortering-work-package\app`
- Zip: `..\LagerPalleSortering-work-package\LagerPalleSortering-work.zip`

På arbejds-PC:
1. Pak zip-filen ud.
2. Kør `Start-Lager.cmd`.
3. Appen starter på `http://127.0.0.1:5050`.

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

## Eksport
- CSV: `GET /export/csv`
- Excel: `GET /export/excel`

## Dokumentation
- Brugerguide: `docs/USER_GUIDE.md`
- Teknisk guide: `docs/TECHNICAL_GUIDE.md`
- Drift/fejlsøgning: `docs/OPERATIONS.md`
