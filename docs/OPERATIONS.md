# Drift og Fejlsøgning

## Drift
- Applikation startes med:

```powershell
dotnet run
```

- Runtime data ligger i: `App_Data/lager.db`
- Tag backup af `lager.db` før opgraderinger eller større driftstiltag.

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

### Ingen u-bekræftede kolli
- Alle registrerede kolli på pallen er allerede bekræftet.

## Release-checkliste
1. `.\scripts\verify.ps1` er grøn.
2. README og docs afspejler aktuelle regler.
3. GitHub Actions `CI` er grøn på seneste commit.
4. Commit + push.

