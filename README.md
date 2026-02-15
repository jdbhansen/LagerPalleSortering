# LagerPalleSortering

Blazor Web App i C# til at holde styr på destinationpaller ved varemodtagelse.

## Funktioner

- Registrer varenummer, holdbarhed (YYYYMMDD) og antal kolli.
- Automatisk pallevalg baseret på `varenummer + holdbarhed`.
- Opretter ny palle (`P-001`, `P-002`, ...) ved ny kombination.
- Viser åbne paller og total kolli per palle.
- Luk palle manuelt, så nye kolli i samme gruppe går til en ny palle.
- Fortryd seneste registrering.
- Persistens i SQLite (`App_Data/lager.db`), så data bevares efter genstart.
- Eksport af data til både CSV og Excel (`.xlsx`).
- Scanner-optimeret flow: Enter hopper felter og kan registrere uden mus.
- Print-label med skanbar stregkode (Code128) for pallens ID.
- Scan palle-stregkode i appen for at bekræfte, at kolli er flyttet.
  Format på nye labels: `PALLET:P-001`.
- Op til 4 forskellige vare+dato-varianter pr. palle.
- Samme stregkode med forskellig holdbarhed må aldrig blandes på samme palle.
- Varescanning understøtter EAN-8, EAN-13 og UPC-A (UPC-A normaliseres til EAN-13).

## Arkitektur

- `Application/`: use-case logik og service-abstraktioner.
  Eksport ligger i separat `WarehouseExportService`.
- `Infrastructure/`: SQLite repository og databaseadgang.
- `Domain/`: kontrakter, result-typer og konstanter.

## Dokumentation

- Brugerguide: `docs/USER_GUIDE.md`
- Teknisk guide: `docs/TECHNICAL_GUIDE.md`
- Drift/fejlsøgning: `docs/OPERATIONS.md`

## Kør lokalt

```powershell
dotnet run
```

Åbn derefter URL'en fra terminalen i browseren.

## Eksport

- CSV: `/export/csv`
- Excel: `/export/excel`

## Tests

```powershell
dotnet test LagerPalleSortering.slnx
```

Kun sanity/smoke tests:

```powershell
dotnet test LagerPalleSortering.slnx --filter "Category=Sanity"
```

