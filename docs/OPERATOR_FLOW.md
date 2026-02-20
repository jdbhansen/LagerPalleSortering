# Operator Flow (React)
Sidst opdateret: 2026-02-20.

## Formål
Dagligt operatørflow i React-frontend (`/app`).

## Ny pallesortering
1. Start ny sortering
2. Scan kolli stregkode
3. Indtast holdbarhed (YYYYMMDD)
4. Registrer kolli
5. Scan palle
6. Sæt kolli på plads
7. Afslut pallesortering når batch er færdig

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
