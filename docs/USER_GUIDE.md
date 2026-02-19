# Brugerguide
Sidst opdateret: 2026-02-19.

## Formål
Appen bruges ved varemodtagelse til at styre, hvilken palle hvert kolli flyttes til.

## Tilstande
- `Ny pallesortering`: fokuseret scannerflow med én aktiv sortering ad gangen
- `Fuld oversigt`: komplet driftsoverblik med tabeller, restore og historik

## Workflow i ny pallesortering
1. Tryk `Start ny pallesortering`
2. Scan/indtast `Kolli stregkode`
3. Indtast `Holdbarhed (YYYYMMDD)`
4. Tryk `Registrer kolli`
5. Scan `Palle stregkode`
6. Tryk `Sæt kolli på plads`
7. Når du er færdig, tryk `Afslut pallesortering`

## Lukning og print
- `Luk palle + print indholdslabel` lukker den foreslåede palle og printer automatisk.
- I `Indhold på paller` kan du:
  - vælge palle (åben/lukket)
  - se varelinjer
  - lukke palle
  - genprinte indholdslabel

## Print
- Print foregår i samme SPA-forløb (ingen nye faner)
- Ruter:
  - `/app/print-label/{palletId}`
  - `/app/print-pallet-contents/{palletId}?format=label190x100`

## Typiske fejl
- `Ugyldig pallestregkode`
- `Ingen u-bekræftede kolli fundet`
- `Holdbarhed skal være 8 cifre i format YYYYMMDD`
