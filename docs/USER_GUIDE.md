# Brugerguide

## Formål
Appen hjælper med at styre, hvilken palle kolli skal flyttes til ved varemodtagelse.

## Dagligt flow
1. Scan/indtast `Varenummer`, `Holdbarhed (YYYYMMDD)` og `Antal kolli`.
2. Tryk `Registrer` (eller Enter via scanner-flow).
3. Følg den foreslåede palle i statusbeskeden.
4. Print label via `Print` på pallen.
5. Scan pallelabel (`PALLET:P-xxx`) for at bekræfte flytning.

## Vigtige regler
- Maks 4 forskellige vare+dato-varianter pr. palle.
- Samme stregkode med forskellig holdbarhed må aldrig blandes på samme palle.
- Tom holdbarhed gemmes som `NOEXP`.

## Stregkoder
- Varekoder: EAN-8, EAN-13, UPC-A.
- UPC-A normaliseres til EAN-13 internt.
- Scannerpræfiks som `]E0` håndteres automatisk.
- Pallelabels bruger format: `PALLET:P-001`.

## Knapper og visninger
- `Luk`: lukker pallen for nye kolli.
- `Fortryd seneste`: ruller sidste registrering tilbage.
- `Eksport CSV` og `Eksport Excel`: henter datafiler.

## Fejlhåndtering
- `Ugyldig pallestregkode`: scan en pallekode i format `PALLET:P-xxx`.
- `Ingen u-bekræftede kolli`: pallen har ingen afventende flyt-bekræftelser.
