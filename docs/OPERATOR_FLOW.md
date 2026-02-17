# Operator Flow (React)
Sidst opdateret: 2026-02-17.

## Formål
Dokumenterer dagligt operator-flow i React-frontend (`/app`).

## Flow
1. Registrer kolli (`Varenummer`, `Holdbarhed`, `Antal kolli`)
2. Flyt fysisk kolli til foreslået palle
3. Bekræft med palle-scan
4. Kontrollér status i tabeller

## Kritiske handlinger
- `Ryd database` kræver eksplicit bekræftelse
- `Gendan database` kræver gyldig backupfil
- `Fortryd seneste` påvirker sidste registrering

## Datostregkode
- Datostregkode er adskilt visuelt fra varestregkode
- Label: `Dato / Holdbarhed`
- Bruges til hurtig gen-scan af dato uden manuel indtastning

## Edge-cases
- Tom pallekode ved bekræftelse: fallback til senest foreslået palle
- Delvis bekræftelse: vises som advarsel
- Ugyldigt antal (`<= 0`): returnerer fejl

## Operatørens kontrolpunkter
- Tjek at statuspanel viser success/warning efter handling
- Tjek at foreslået palle matcher fysisk palle før flyt
- Tjek at bekræftet antal stiger korrekt ved batch-scan

## Relaterede dokumenter
- Brugerguide: [`docs/USER_GUIDE.md`](USER_GUIDE.md)
- Drift: [`docs/OPERATIONS.md`](OPERATIONS.md)
- Teknisk guide: [`docs/TECHNICAL_GUIDE.md`](TECHNICAL_GUIDE.md)
