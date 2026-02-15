# Drift og Fejlsøgning

## Kørsel
```powershell
dotnet run
```

## Data
- Databasefil: `App_Data/lager.db`
- Tag backup af filen før større ændringer eller opdateringer.

## Build og test
```powershell
dotnet build LagerPalleSortering.slnx
dotnet test LagerPalleSortering.slnx --no-build
```

## Kendt problem: fil-lock
Hvis `dotnet test` fejler med `CS2012` / låst DLL:
1. Kør build først:
   `dotnet build LagerPalleSortering.slnx`
2. Kør test uden rebuild:
   `dotnet test LagerPalleSortering.slnx --no-build`

Hvis låsen fortsætter:
```powershell
Get-CimInstance Win32_Process | Where-Object {
  ($_.Name -match 'dotnet|testhost') -and ($_.CommandLine -match 'LagerPalleSortering')
} | Select-Object ProcessId,Name,CommandLine
```
Stop de relevante processer og kør build/test igen.

## Versionsstyring
- Commit kun efter grøn build + tests.
- Kør sanity-tests før hurtig release.
