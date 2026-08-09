# Candidates Service

Bounded context: **talent pool** (client-facing Professional/Student; technical `job_seeker`).
This service is the **reference vertical slice** for Illumin360 — every other service mirrors its layout.

## Layers (Clean Architecture)
- `Illumin360.Candidates.Domain` — `Candidate` aggregate, value objects, domain events. No dependencies.
- `Illumin360.Candidates.Application` — CQRS handlers, ports (`ICandidateRepository`), DTOs.
- `Illumin360.Candidates.Infrastructure` — EF Core `CandidatesDbContext`, repository, DI.
- `Illumin360.Candidates.Api` — Minimal API composition root: endpoints, OpenAPI, health, OTel.

## Run
```bash
# from repo root, with infra up (docker compose ... up -d)
dotnet run --project src/Services/Candidates/Illumin360.Candidates.Api
```

| Endpoint | Purpose |
| --- | --- |
| `GET /v1/candidates?city=&page=&pageSize=` | List candidates (paged). |
| `GET /health/live` | Liveness (process up). |
| `GET /health/ready` | Readiness (DB reachable). |
| `GET /health/startup` | Startup (migrations applied). |
| `GET /openapi/v1.json` | OpenAPI 3.1 contract. |

## Ports
- HTTP: `8080` (container) / `5201` (local dev)
- Database: `illumin360_candidates` on PostgreSQL

## Telemetry / Runbook
- OTLP → Alloy → Grafana LGTM. Dashboard: `06-operations/dashboards/candidates.json`.
- Runbook: `06-operations/runbooks/candidates.md`.
