# Operator Flow

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering: Fast commit/push tjekliste er oprettet og linket fra centrale docs (2026-02-21).





## Formål

Kort, operationel guide til skannere og palleflow i skiftet.

## Start af skift

1. Åbn `/app`.
2. Vælg printer via `Vælg/skift printer` i `Ny pallesortering` (én gang pr. station/skift ved behov).
3. Scan en kendt pallelabel (fx `PALLET:P-001`) som sanity check.
4. Bekræft at scanner læser print tydeligt.

## Standardflow (Ny pallesortering)

1. Start ny sortering.
2. Trin 1: scan kolli + holdbarhed.
3. Registrer kolli.
4. Trin 2: scan pallelabel.
5. Sæt kolli på plads.
6. Gentag fra trin 1.
7. Afslut sortering.

## Print-flow

- Ny palle kan automatisk åbne label-print.
- `Luk palle + print indholdslabel` bruges når pallen er komplet.
- Alle print indeholder udskriftstidspunkt.
- Ved retur fra print genoptages flowet på aktivt trin.
- Kør browseren med kiosk-printing for at undgå OK-dialog på hvert print.

## Hurtig fejlafklaring

- `Ugyldig pallestregkode`:
  - Scan igen
  - Tjek `:` vs `æ`, `-` vs `+`
- `Ingen u-bekræftede kolli fundet`:
  - Tjek at korrekt palle er scannet
  - Tjek om kolli allerede er sat på plads
- Datofejl:
  - Brug `YYYYMMDD` eller gyldig `YYMMDD`

## Slut af skift

1. Luk aktive paller efter behov.
2. Tag backup ved større ændringer.
3. Notér scanner-afvigelser til næste skift.
