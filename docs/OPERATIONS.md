# Drift og Fejlsøgning
Sidst opdateret: 2026-02-17.

## Drift
Start app:

```powershell
dotnet run
```

- Primær UI: `/app`
- Legacy UI: `/legacy`
- Data: `App_Data/lager.db`

## Daglig drift
- Brug backup før større ændringer: `GET /backup/db`
- Kontroller status via:
  - `GET /health`
  - `GET /metrics`

## Build og test (anbefalet)

```powershell
./scripts/verify.ps1
```

Supplerende frontend/e2e:

```powershell
cd frontend
npm run lint
npm run test
npm run build
cd ..
npm run test:e2e
```

## Work package

```powershell
./scripts/package-work.ps1
```

Output:
- `..\LagerPalleSortering-work-package\app`
- `..\LagerPalleSortering-work-package\LagerPalleSortering-work.zip`

Work package er self-contained og kræver ikke installation af .NET/Node på arbejds-pc.

## Fejlsøgning
### React-side loader ikke
- Verificér at `wwwroot/app/index.html` og `wwwroot/app/assets/*` findes.
- Kør `cd frontend && npm run build` igen.

### Print af palleindhold klippes i bund
- Brug `190x100`-knappen (`?format=label190x100`) for SVG-baseret labelprint.
- Standardprint arver printerens papir/margin-indstillinger.

### Fil-lock i build/test
- Kør `./scripts/verify.ps1`.
- Gentag build/test når låsende `testhost/dotnet` processer er stoppet.

### Scannerfejl i pallekode
- Bekræft format `PALLET:P-xxx`.
- Appen normaliserer kendte layout-afvigelser (`æ/Æ`, `+`).
- Match scanner keyboard-country med Windows layout.

## Release-checkliste
1. `./scripts/verify.ps1` grøn
2. `cd frontend && npm run lint && npm run test && npm run build`
3. `npm run test:e2e` grøn
4. `./scripts/package-work.ps1` kørt
5. Dokumentation opdateret

## CI-note
- Workflow bruger `concurrency.cancel-in-progress: true`.
- Ældre runs kan derfor stå som `cancelled`, når nyere commit-run starter.

## Relaterede dokumenter
- Projektoversigt: [`README.md`](../README.md)
- Brugerguide: [`docs/USER_GUIDE.md`](USER_GUIDE.md)
- Operator-flow: [`docs/OPERATOR_FLOW.md`](OPERATOR_FLOW.md)
- Teknisk guide: [`docs/TECHNICAL_GUIDE.md`](TECHNICAL_GUIDE.md)
