# Migration Notes
Sidst opdateret: 2026-02-20.

## Formål
Dette dokument beskriver status for dependency-opdateringer og forsøget på ESLint 10 migration.

## Branches og commits
- Stabil opdatering (beholdt): `feat/react-ny-pallesortering-view`
  - Commit: `a8e1408`
  - Indhold: frontend dependencies opdateret til nyeste kompatible versioner.
- Eksperiment (separat): `spike/eslint10-migration`
  - Commit: `cacee10`
  - Indhold: forsøg på at migrere til ESLint 10.

## Nuværende stabil status
- Frontend:
  - `npm --prefix frontend run lint` passerer
  - `npm --prefix frontend run test` passerer
  - `npm --prefix frontend run build` passerer
- Backend:
  - `dotnet build` passerer
  - `dotnet test` passerer
  - `dotnet list LagerPalleSortering.csproj package --vulnerable --include-transitive` finder ingen sårbare NuGet-pakker

## ESLint 10 spike resultat
- `eslint@10` kan installeres, men lint fejler i praksis i nuværende plugin-kæde.
- Reproducerbar fejl:
  - `TypeError: Class extends value undefined is not a constructor or null`
  - optræder ved `npm --prefix frontend run lint` på spike-branch.

## Hvorfor migration er blokeret lige nu
- `typescript-eslint` dokumenterer aktuelt officiel support for ESLint `^8.57.0 || ^9.0.0` (ikke ESLint 10 endnu).
- Projektet bruger `typescript-eslint` i flat config, så lint er direkte afhængig af den kompatibilitet.

## Audit status (frontend)
- `npm --prefix frontend audit` viser advisories i dev-tooling-kæden (ESLint/transitive dependencies).
- Da ESLint 10 ikke er stabilt kompatibel i vores stack endnu, kan advisories ikke fjernes uden at bryde lint.
- Runtime-funktionalitet (build/test/e2e) er intakt.

## Anbefalet plan
1. Hold produktion på den stabile dependency-linje (`a8e1408`).
2. Kør periodisk re-check af:
   - `typescript-eslint` release notes
   - `npm --prefix frontend audit`
3. Når `typescript-eslint` officielt understøtter ESLint 10:
   - opgrader `eslint` + `@eslint/js` til 10.x
   - opdater relaterede plugins
   - valider: lint, test, build, e2e.

## Evidensgrundlag
- Noten er baseret på:
  - output fra `npm --prefix frontend run lint`
  - output fra `npm --prefix frontend audit`
  - output fra `npm --prefix frontend audit fix --force` (peer dependency konflikter)
