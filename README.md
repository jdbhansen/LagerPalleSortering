# LagerPalleSortering

Intern app til varemodtagelse og palle-styring i lagerdrift.
Stack: React frontend + ASP.NET backend + SQLite.
Sidst opdateret: 2026-02-20.

## Start her
- Kør app lokalt: `dotnet run`
- Kør samlet verificering: `./scripts/verify.ps1`
- Byg frontend: `npm --prefix frontend run build`

## Dokumentation
- Brugerguide: [`docs/USER_GUIDE.md`](docs/USER_GUIDE.md)
- Operator-flow: [`docs/OPERATOR_FLOW.md`](docs/OPERATOR_FLOW.md)
- Teknisk guide: [`docs/TECHNICAL_GUIDE.md`](docs/TECHNICAL_GUIDE.md)
- Drift/fejlsøgning: [`docs/OPERATIONS.md`](docs/OPERATIONS.md)
- Frontend-noter: [`frontend/README.md`](frontend/README.md)

## Nøglefunktioner
- React SPA på `/app` med to tilstande: `Ny pallesortering` og `Fuld oversigt`
- Ét aktivt sorteringsforløb ad gangen med eksplicit `Afslut pallesortering`
- Scannerflow: registrer kolli -> scan palle -> bekræft flyt
- GS1/QR parsing i registrering: læser `AI(01)` varenummer og `AI(17)` holdbarhed automatisk
- Automatisk dato-normalisering: `YYMMDD` konverteres til gyldig `YYYYMMDD` ved registrering
- Auto-print af pallelabel, når en ny palle oprettes
- Luk palle og auto-print palle-indholdslabel (`190x100`)
- Sektion `Indhold på paller` i begge visninger (inkl. genprint af lukkede paller)
- Event-baseret opdatering af `Indhold på paller` efter relevante handlinger (ingen konstant polling)
- API under `/api/warehouse/*`, eksport/backup/health under egne endpoints

## Arkitektur
- `frontend/`: React SPA, feature-opdelt med hooks, kontrakter og route-helpers
- `Api/`: Minimal API endpoints (`WarehouseApiEndpoints`, `OperationalApiEndpoints`)
- `Application/`: use-cases og services
- `Domain/`: forretningsregler og barcode-kontrakter
- `Infrastructure/`: SQLite repository
- `wwwroot/app`: deployet React SPA build

## Endpoints
- `GET /api/warehouse/dashboard`
- `GET /api/warehouse/pallets/{palletId}`
- `GET /api/warehouse/pallets/{palletId}/contents`
- `POST /api/warehouse/register`
- `POST /api/warehouse/confirm`
- `POST /api/warehouse/pallets/{palletId}/close`
- `POST /api/warehouse/undo`
- `POST /api/warehouse/clear`
- `POST /api/warehouse/restore`
- `GET /export/csv`
- `GET /export/excel`
- `GET /backup/db`
- `GET /health`
- `GET /metrics`
