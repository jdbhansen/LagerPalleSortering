# Brugerguide

## Hurtig Navigation
- [Standard workflow](#standard-workflow)
- [Regler i systemet](#regler-i-systemet)
- [Scannerregler](#scannerregler)
- [Funktioner i UI](#funktioner-i-ui)
- [Typiske fejlbeskeder](#typiske-fejlbeskeder)
- [Relaterede dokumenter](#relaterede-dokumenter)

## Hvad appen bruges til
LagerPalleSortering bruges ved varemodtagelse til at styre, hvilken palle hvert kolli skal flyttes til.

## Standard workflow
1. Registrer kolli
   Scan/indtast `Varenummer`, `Holdbarhed (YYYYMMDD, gyldig dato)` og `Antal kolli`.
2. Flyt kolli
   Følg foreslået palle i statusfeltet.
3. Label og bekræft
   Print pallelabel og scan label (`PALLET:P-xxx`) for at bekræfte fysisk flytning.

## Regler i systemet
- Maks 4 forskellige vare+dato-varianter pr. palle.
- Samme stregkode med forskellig holdbarhed må aldrig placeres på samme palle.
- Tom holdbarhed lagres som `NOEXP`.

## Scannerregler
- Varescan: EAN-8, EAN-13, UPC-A.
- UPC-A normaliseres automatisk til EAN-13.
- Prefix fra scanner-symbologi (fx `]E0`) understøttes.
- Pallelabels forventes i format `PALLET:P-001`.
- Ved palle-scan ignoreres scanner-støj:
  - `PALLETæP-001` / `PALLETÆP-001` tolkes som `PALLET:P-001` (layout mismatch).
  - `P+001` tolkes som `P-001`.
  - ekstra tegn (fx `æ`, mellemrum, symboler) filtreres automatisk væk.
- Anbefalet opsætning: scanner keyboard-layout og Windows input-sprog bør matche (fx begge Danish eller begge US).

## Funktioner i UI
- `Skift til simpel scanner-visning`: viser en forenklet side med store felter/knapper til håndscanner-flow.
- `Registrer`: opretter/tilføjer til relevant palle.
- `Print`: udskriver label for valgt palle.
- `Bekræft`: bekræfter flytning via palle-scan.
- `Luk`: lukker palle for yderligere tilføjelser.
- `Fortryd seneste`: ruller sidste registrering tilbage.
- `Backup DB`: downloader en komplet database-backup (`.db`).
- `Gendan database`: indlæs backupfil direkte i appen (sektionen ligger nederst på siden).
- `Eksport CSV` / `Eksport Excel`: henter driftsdata.
- `Statusfelt`: vises tydeligt og centreret øverst på siden med succes/advarsels/fejlstatus.

## Typiske fejlbeskeder
- `Scan ignoreret: samme palle blev allerede scannet lige før.`
  Samme palle blev scannet igen inden for guard-vinduet. Vent et øjeblik og scan igen.
- `Ugyldig pallestregkode...`
  Scan en pallekode med palle-id (`P-xxx`) i data. Appen accepterer både `PALLET:P-xxx` og støjfyldte scans, så længe palle-id kan udtrækkes.
- `Ingen u-bekræftede kolli...`
  Alle registrerede kolli på pallen er allerede bekræftet.

## Relaterede dokumenter
- Projektoversigt: [`README.md`](../README.md)
- Teknisk guide: [`docs/TECHNICAL_GUIDE.md`](TECHNICAL_GUIDE.md)
- Drift/fejlsøgning: [`docs/OPERATIONS.md`](OPERATIONS.md)
