# Brugerguide

Sidst opdateret: 2026-02-20.

## Formål

Appen bruges i varemodtagelsen til at registrere kolli, vælge palle og bekræfte flytning.

## Visninger

- `Ny pallesortering`: fokuseret scannerflow med ét aktivt trin ad gangen.
- `Fuld oversigt`: driftsvisning med tabeller, historik og databaseværktøjer.

## Ny pallesortering (anbefalet flow)

1. Klik `Start ny pallesortering`.
2. Trin 1: scan/indtast `Kolli stregkode` og holdbarhed.
3. Klik `Registrer kolli`.
4. Hvis ny palle oprettes, printes label.
5. Trin 2: scan pallelabel og klik `Sæt kolli på plads`.
6. Gentag fra trin 1.
7. Klik `Afslut pallesortering`, når batchen er færdig.

Bemærk:
- Hvis du går til print-view og tilbage igen, genoptages trin 2 automatisk for den ventende palle.
- Inputfelter i ny sortering har `autocomplete` slået fra.

## Scanning og dato

- GS1/QR med `AI(01)` + `AI(17)` udfylder automatisk varenummer + holdbarhed.
- Manuel holdbarhed accepterer:
  - `YYYYMMDD`
  - gyldig `YYMMDD` (normaliseres til `YYYYMMDD`)
- Ugyldig dato afvises med fejltekst.

## Lukning og print

- `Luk palle + print indholdslabel` lukker foreslået palle og åbner printvisning.
- I `Indhold på paller` kan du vælge palle, se indhold, lukke palle og genprinte.

## Typiske fejlbeskeder

- `Scan kolli stregkode først.`
- `Holdbarhed skal være 8 cifre i format YYYYMMDD.`
- `Ugyldig pallestregkode.`
- `Ingen u-bekræftede kolli fundet for palle ...`

## Hjælp ved scannerproblemer

Se `docs/SCANNER_VALIDATION.md` for komplet valideringscheckliste.
