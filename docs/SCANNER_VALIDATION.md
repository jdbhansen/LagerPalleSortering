# Scanner Validation

Sidst opdateret: 2026-02-20.

## Formål

Sikre at stregkoder fungerer stabilt på tværs af scanner-modeller, keyboard-layouts og printkvalitet.

## Automatisk validering i kode

- Backend scanner-kompatibilitet:
  - `tests/LagerPalleSortering.Tests/BarcodeScannerCompatibilityTests.cs`
- Frontend payload-validering:
  - `frontend/src/features/warehouse/utils/palletBarcodePayload.test.ts`

Kør tests:

```powershell
dotnet test
npm --prefix frontend run test -- --run
```

## Manuel validering på hardware

1. Test mindst 3 scanner-profiler:
   - Handheld 1D
   - Presentation scanner
   - Scanner med alternativ keyboard-layout profil
2. Test payloads:
   - `PALLET:P-001`
   - `PALLET:P-010`
   - `PALLET:P-999`
3. Bekræft tolerance for scanner-støj:
   - Prefix (`]E0`, `]C1`)
   - `+` i stedet for `-`
   - `Æ/æ` i stedet for `:`
4. Printkvalitet:
   - 100%, 90%, 80% skalering
   - blank vs. mat label
   - kort og lang scanafstand
5. Keyboard-wedge output:
   - ingen ekstra newline/tab
   - korrekt tegnmapning

## Acceptance criteria

- Alle automatiske barcode-tests er grønne.
- Alle testpayloads scannes korrekt på hver scanner-model.
- Ingen uventede parser-fejl i normal drift.
