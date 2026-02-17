# Drift og Fejlsøgning

## Hurtig Navigation
- [Drift](#drift)
- [Build og test (anbefalet)](#build-og-test-anbefalet)
- [Manuelle kommandoer](#manuelle-kommandoer)
- [Fejlsøgning](#fejlsøgning)
- [Release-checkliste](#release-checkliste)
- [Relaterede dokumenter](#relaterede-dokumenter)

## Drift
- Applikation startes med:

```powershell
dotnet run
```

- Runtime data ligger i: `App_Data/lager.db`
- Tag backup af `lager.db` før opgraderinger eller større driftstiltag.
- Appen har også indbygget backup-download: `GET /backup/db`.
- Health/metrics:
  - `GET /health`
  - `GET /metrics`
- Restore-funktionen i UI ligger nederst på forsiden for at holde dagligt scan-flow adskilt fra driftsindgreb.
- UI har en toggle til `Simpel scanner-visning` for hurtig håndscanner-betjening; brug `Avanceret visning` ved behov for eksport/restore/tabeller.

## Build og test (anbefalet)
Brug projektets verifikationsscript:

```powershell
.\scripts\verify.ps1
```

Scriptet:
- stopper hængende `testhost/dotnet` processer for dette repo.
- kører `dotnet build`.
- kører `dotnet test --no-build`.
- stopper igen `testhost/dotnet` processer for repoet efter kørsel (cleanup).

UI sanity (Playwright):
```powershell
npm ci
npx playwright install chromium
npm run test:e2e
```

## Manuelle kommandoer
```powershell
dotnet build LagerPalleSortering.slnx
dotnet test LagerPalleSortering.slnx --no-build
```

Sanity-only:
```powershell
dotnet test LagerPalleSortering.slnx --filter "Category=Sanity"
```

## Fejlsøgning
### Fil-lock (CS2012 / låst DLL)
1. Kør `.\scripts\verify.ps1`.
2. Hvis problemet fortsætter, find låsende processer:

```powershell
Get-CimInstance Win32_Process | Where-Object {
  ($_.Name -match 'dotnet|testhost') -and ($_.CommandLine -match 'LagerPalleSortering')
} | Select-Object ProcessId,Name,CommandLine
```

3. Stop relevante processer og kør verify igen.

### Ugyldig pallescan
- Kontroller at label er i format `PALLET:P-xxx`.
- Appen filtrerer scanner-støj (fx `+`, `æ`, symboler), men der skal stadig indgå et palle-id (`P-xxx`) i den scannede tekst.
- Ved keyboard-layout mismatch (typisk US scanner + dansk Windows) kan `:` blive til `æ`; appen tolererer dette i palle-scan.
- Bekræft-knappen læser input-felternes aktuelle råværdi direkte ved submit for at reducere timing-relaterede scannerfejl.
- Drift-anbefaling: sæt scanner keyboard-country til samme layout som Windows for at undgå sideeffekter i andre inputfelter.

### Ingen u-bekræftede kolli
- Alle registrerede kolli på pallen er allerede bekræftet.

## Release-checkliste
1. `.\scripts\verify.ps1` er grøn.
2. `npm run test:e2e` er grøn (lokalt eller i CI).
3. README og docs afspejler aktuelle regler.
4. GitHub Actions `CI` er grøn på seneste commit.
5. Commit + push.

## Relaterede dokumenter
- Projektoversigt: [`README.md`](../README.md)
- Brugerguide: [`docs/USER_GUIDE.md`](USER_GUIDE.md)
- Teknisk guide: [`docs/TECHNICAL_GUIDE.md`](TECHNICAL_GUIDE.md)

