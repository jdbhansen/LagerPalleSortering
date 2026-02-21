# Branch Policy

Sidst opdateret: 2026-02-21.

## Mål

Hold `master` stabil med ensartede kvalitetsgates.

## Anbefalet GitHub-opsætning

1. Beskyt `master` branch.
2. Kræv pull request før merge.
3. Kræv mindst 1 review.
4. Kræv grønne checks:
- `backend`
- `frontend`
- `e2e`
5. Slå direkte pushes til `master` fra.
6. Slå "Dismiss stale approvals" til.
7. Kræv up-to-date branch før merge.

## Lokal forventning før PR

- Kør samme checks som CI.
- Opdater relevant dokumentation.
- Beskriv rollback-plan i PR.
