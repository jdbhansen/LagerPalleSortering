# Scanner Validation

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering: test- og arkitektursektioner er opdateret (2026-02-21).

## Formål

Sikre stabile scans på tværs af scanner-modeller, keyboard-layouts og labelkvalitet.

## Automatisk validering (skal altid være grøn)

- Backend:
  - `tests/LagerPalleSortering.Tests/BarcodeScannerCompatibilityTests.cs`
- Frontend:
  - `frontend/src/features/warehouse/utils/palletBarcodePayload.test.ts`

Kør:

```powershell
dotnet test
npm --prefix frontend run test
```

## Manuel hardware-validering

1. Test mindst 3 profiler:
- Handheld 1D
- Presentation scanner
- Scanner med alternativ keyboard-wedge layout

2. Test payloads:
- `PALLET:P-001`
- `PALLET:P-010`
- `PALLET:P-999`

3. Test støjtolerance:
- Prefix: `]E0`, `]C1`
- `+` i stedet for `-`
- `æ/Æ` i stedet for `:`

4. Test printkvalitet:
- 100%, 90%, 80% skala
- blank og mat label
- kort og lang læseafstand

5. Test keyboard output:
- ingen ekstra newline/tab
- korrekt tegnmapning

## Acceptance criteria

- Alle automatiske barcode-tests passerer.
- Alle testpayloads kan scannes på alle godkendte scannerprofiler.
- Ingen parser-fejl i normal drift.
- Udskrifter er læsbare og indeholder udskriftstidspunkt.

