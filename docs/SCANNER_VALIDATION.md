# Scanner Validation Checklist

Formaal: Verificer at genererede stregkoder fungerer stabilt pa tværs af scanner-modeller og opsætninger.

## Automatisk validering i kode
- Backend parser/noise-kompatibilitet:
  - `tests/LagerPalleSortering.Tests/BarcodeScannerCompatibilityTests.cs`
- Frontend canonical payload-validering:
  - `frontend/src/features/warehouse/utils/palletBarcodePayload.test.ts`
- Kør hele pakken:
  - `dotnet test`
  - `npm test -- --run`

## Manuel validering pa scanner-hardware
1. Test mindst 3 scanner-profiler:
   - Handheld 1D
   - Presentation scanner
   - En scanner med anden keyboard-layout profil
2. Scan disse payloads:
   - `PALLET:P-001`
   - `PALLET:P-010`
   - `PALLET:P-999`
3. Bekraeft at parser accepterer scanner-variationer:
   - Prefix-stoej (`]E0`, `]C1`)
   - `+` i stedet for `-`
   - `Æ/æ` i stedet for `:`
4. Print-kvalitet:
   - Test ved 100%, 90%, 80% skalering
   - Test blank vs. mat label
   - Verificer scanning ved kort og lang afstand
5. Keyboard-wedge output:
   - Bekraeft at data ankommer uden ekstra newline/tab
   - Bekraeft at locale/keyboard mapning ikke korrupts tegn

## Acceptance criteria
- 0 fejl i automatiske barcode-tests.
- 100% successful scan for testpayloads pa hver scanner-model.
- Ingen uventede parse-fejl ved scanner-stoejscenarier.
