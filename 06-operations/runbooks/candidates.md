# Runbook — Candidates Service

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-05-30 · **Related ADRs:** ADR-0004, ADR-0007, ADR-0008

**Purpose:** talent-pool reads/writes. **Ports:** 8080 (container) / 5201 (local). **DB:** illumin360_candidates
(schema `candidates`: aggregate + MassTransit outbox tables). **Broker:** RabbitMQ (publishes `CandidateRegistered`).

## Endpoints
- `POST /v1/candidates` (register) · `GET /v1/candidates/{id}` · `GET /v1/candidates?city=` (ILIKE filter).
- Via gateway: `/api/candidates/**` → rewritten to `/v1/candidates/**`.

## Deploy / rollback
- Deploy: `docker compose ... up -d --build candidates-api` (local) / Helm chart (k8s).
- Migrations apply automatically at startup (`MigrateAsync`); they must be backward-compatible (expand/contract).
- Rollback: redeploy previous image tag. **Note:** a DB first created by the legacy `EnsureCreated` bootstrap
  has no `__EFMigrationsHistory` — drop & recreate the `candidates` schema once so migrations can take over.

## Probes
- `/health/live` process · `/health/ready` (DB + `masstransit-bus`) · `/health/startup` migrations applied.

## Common alerts → response
| Alert | Likely cause | First response |
| --- | --- | --- |
| High 5xx rate (RED) | DB down / bad deploy | Check `/health/ready`; inspect Loki logs by TraceId; roll back if deploy-correlated |
| p95 latency breach | DB slow / N+1 | Check Tempo traces; review EF queries; scale replicas |
| Readiness failing | Postgres unreachable | Verify postgres container + connection string secret in Vault |

Dashboard: `06-operations/dashboards/candidates.json`. Telemetry: source `candidates`.
