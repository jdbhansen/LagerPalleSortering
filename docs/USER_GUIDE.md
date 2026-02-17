# Brugerguide

## Hvad appen bruges til
LagerPalleSortering bruges ved varemodtagelse til at styre, hvilken palle hvert kolli skal flyttes til.

## Standard workflow
1. Registrer kolli
   Scan/indtast `Varenummer`, `Holdbarhed (YYYYMMDD)` og `Antal kolli`.
2. Flyt kolli
   Følg foreslået palle i statusfeltet.
3. Bekræft flyt
   Scan pallelabel (`PALLET:P-xxx`) og bekræft.

## UI-funktioner
- `Skift til simpel scanner-visning`
- `Eksport CSV` / `Eksport Excel`
- `Backup DB` / `Gendan database`
- `Fortryd seneste`
- `Luk` / `Luk + print`

## Datostregkode
- Holdbarhedsfeltet kan generere en **datostregkode**.
- Datostregkoden er visuelt markeret som `Dato / Holdbarhed` for ikke at forveksles med varestregkode.
- Datostregkoden kan printes og scannes igen senere.

## Scannerregler
- Varescan: EAN-8, EAN-13, UPC-A
- UPC-A normaliseres til EAN-13
- Palle-scan tåler scanner-støj (`æ/Æ`, `+`, ekstra tegn)

## Typiske fejlbeskeder
- `Scan ignoreret...` (dubletscan)
- `Ugyldig pallestregkode...`
- `Ingen u-bekræftede kolli...`

## Gode driftsvaner
- Brug altid scanner i samme tastaturlayout som Windows-maskinen
- Tag backup før større ændringer eller nulstilling
- Bekræft antal i tabellen efter batch-scan

## Relaterede dokumenter
- Projektoversigt: [`README.md`](../README.md)
- Operator-flow: [`docs/OPERATOR_FLOW.md`](OPERATOR_FLOW.md)
- Teknisk guide: [`docs/TECHNICAL_GUIDE.md`](TECHNICAL_GUIDE.md)
- Drift: [`docs/OPERATIONS.md`](OPERATIONS.md)
