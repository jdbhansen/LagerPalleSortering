# Commit Push Checklist

Sidst opdateret: 2026-02-21.

Seneste dokument-opdatering: Fast commit/push tjekliste er oprettet og linket fra centrale docs (2026-02-21).

Fast arbejdsgang ved `commit push`.

## 1. Før commit

- Opdater relevante `.md`/README filer.
- Kør relevante checks for ændringen:
  - `dotnet build`
  - `dotnet test`
  - `npm --prefix frontend run lint`
  - `npm --prefix frontend run test`
  - `npm --prefix frontend run build`
  - `npm run test:e2e` (når ændringen kan påvirke flow/print/routing)
- Verificer `git status`, så kun relevante filer kommer med.

## 2. Commit

- Stage filer eksplicit (undgå `git add .` ved tvivl).
- Skriv en tydelig commit-besked:
  - `feat: ...`
  - `fix: ...`
  - `test: ...`
  - `docs: ...`
  - `chore: ...`
- Commit:
  - `git commit -m "<type>: <kort beskrivelse>"`

## 3. Push

- Push aktiv branch:
  - `git push origin <branch>`
- Verificer at `HEAD` matcher remote:
  - `git rev-parse HEAD`
  - `git rev-parse origin/<branch>`
- Bekræft ren working tree:
  - `git status --short`

## 4. Efter push

- Opdater work package, hvis ændringen kræver det:
  - `./scripts/package-work.ps1`
- Bekræft at dokumentation stadig matcher implementeringen.

