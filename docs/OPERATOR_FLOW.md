# Operator Flow (React)
Sidst opdateret: 2026-02-20.

## Formål
Dagligt operatørflow i React-frontend (`/app`).

## Ny pallesortering
1. Start ny sortering
2. Scan kolli stregkode
3. GS1/QR-scans (AI 01 + AI 17) udfylder automatisk varenummer + holdbarhed
4. Ved manuel dato kan både `YYYYMMDD` og `YYMMDD` bruges (auto-normaliseres når gyldig)
5. Registrer kolli
6. Scan palle
7. Sæt kolli på plads
8. Afslut pallesortering når batch er færdig

## Lukning og label
- `Luk palle + print indholdslabel` lukker foreslået palle og printer automatisk.
- I `Indhold på paller` kan valgt palle lukkes og indholdslabel printes igen.
- Lukkede paller fjernes ikke fra listen med det samme, så genprint er muligt.
- Indholdslisten opdateres event-baseret efter registrering, flyttebekræftelse og pallelukning.

## Fuld oversigt
- Viser åbne paller, seneste entries og databaseværktøjer.
- Samme sektion `Indhold på paller` findes også her.

## Kritiske handlinger
- `Ryd database` kræver bekræftelse.
- `Gendan database` kræver gyldig backupfil.
- `Fortryd seneste` ændrer sidste registrering.

## Printadfærd
- Print foregår i SPA-ruter under `/app/print-*`.
- Ingen nye browserfaner i normal drift.
