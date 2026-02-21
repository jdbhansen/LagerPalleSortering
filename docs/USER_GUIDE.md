# Brugerguide

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering: test- og arkitektursektioner er opdateret (2026-02-21).

## Formål

Guide til daglig brug i varemodtagelsen.

## Vælg visning

- `Ny pallesortering`: anbefalet flow med ét aktivt trin ad gangen
- `Fuld oversigt`: driftsdashboard med tabeller og værktøjer

## Login

Du skal logge ind før brug af systemet.

## Standardflow: Ny pallesortering

1. Klik `Start ny pallesortering`.
2. Trin 1: scan/indtast `Kolli stregkode` og `Holdbarhed`.
3. Klik `Registrer kolli`.
4. Trin 2: scan `Palle stregkode`.
5. Klik `Sæt kolli på plads`.
6. Gentag trin 2-5 for næste kolli.
7. Klik `Afslut pallesortering` når batchen er færdig.

Note:
- Du kan bekræfte flyt i trin 2 med en scannet pallekode, også hvis der ikke vises en foreslået palle.

## Print

- Ny palle kan udløse label-print automatisk.
- `Luk palle + print indholdslabel` lukker palle og åbner printvisning.
- Alle print viser nu `Udskrevet: <dato/tid>`.
- Når du går tilbage fra print, genoptages aktivt flow.
- Vælg printer via `Vælg/skift printer` i toppen af `Ny pallesortering`.
- For drift uden OK-dialog på hvert print skal stationen starte browseren med kiosk-printing (`./scripts/start-kiosk-print.ps1`).

## Inputregler

- Holdbarhed accepterer:
  - `YYYYMMDD`
  - gyldig `YYMMDD` (auto-normaliseres til `YYYYMMDD`)
- GS1 payload (`AI(01)` + `AI(17)`) kan udfylde felter automatisk.
- Ugyldige værdier blokeres med fejlbesked.

## Typiske fejl og hurtig handling

- `Scan kolli stregkode først.`: udfyld/scann kolli før registrering
- `Holdbarhed skal være 8 cifre ...`: ret datoformat
- `Ugyldig pallestregkode.`: scan pallelabel igen
- `Ingen u-bekræftede kolli ...`: kolli er allerede bekræftet eller forkert palle

## Når scanner driller

Se `docs/SCANNER_VALIDATION.md` for testplan og fejlsøgning.

