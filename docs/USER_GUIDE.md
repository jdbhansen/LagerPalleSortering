# Brugerguide

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
  - `P+001` tolkes som `P-001`.
  - ekstra tegn (fx `æ`, mellemrum, symboler) filtreres automatisk væk.

## Funktioner i UI
- `Registrer`: opretter/tilføjer til relevant palle.
- `Print`: udskriver label for valgt palle.
- `Bekræft`: bekræfter flytning via palle-scan.
- `Luk`: lukker palle for yderligere tilføjelser.
- `Fortryd seneste`: ruller sidste registrering tilbage.
- `Backup DB`: downloader en komplet database-backup (`.db`).
- `Gendan database`: indlæs backupfil direkte i appen.
- `Audit log`: viser seneste kritiske handlinger i UI.
- `Eksport CSV` / `Eksport Excel`: henter driftsdata.

## Typiske fejlbeskeder
- `Scan ignoreret: samme palle blev allerede scannet lige før.`
  Samme palle blev scannet igen inden for guard-vinduet. Vent et øjeblik og scan igen.
- `Ugyldig pallestregkode...`
  Scan en pallekode med palle-id (`P-xxx`) i data. Appen accepterer både `PALLET:P-xxx` og støjfyldte scans, så længe palle-id kan udtrækkes.
- `Ingen u-bekræftede kolli...`
  Alle registrerede kolli på pallen er allerede bekræftet.
