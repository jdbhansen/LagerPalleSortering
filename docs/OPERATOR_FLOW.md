# Operator Flow

Sidst opdateret: 2026-02-20.

## Formål

Kort driftsflow til operatører i daglig varemodtagelse.

## Standardflow (Ny pallesortering)

1. Start sortering.
2. Trin 1: scan kolli + holdbarhed.
3. Registrer kolli.
4. Trin 2: scan pallelabel.
5. Bekræft flytning.
6. Gentag trin 2-5 per kolli.
7. Afslut sortering.

## Print-flow

- Ny palle kan udløse automatisk label-print.
- Efter print kan operatøren stadig gennemføre trin 2 (flytning), fordi ventende palle-id bevares i sessionen.
- `Luk palle + print indholdslabel` bruges når pallen er færdig.

## Fejlflow (hurtig beslutning)

- `Ugyldig pallestregkode`:
  - Scan label igen
  - Tjek scanner-layout (`:`/`æ`, `-`/`+`)
- `Ingen u-bekræftede kolli fundet`:
  - Tjek at korrekt palle er scannet
  - Tjek om kolli allerede er bekræftet
- Datofejl:
  - Brug `YYYYMMDD` eller gyldig `YYMMDD`

## Operatør-checkliste pr. skift

- Test én kendt pallelabel (`PALLET:P-001`) ved skiftstart.
- Bekræft at print er læsbart med scanner.
- Tag backup før større dataoperationer.
