# Runbook — Recruitment Service

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-06-11 · **Related ADRs:** ADR-0004, ADR-0007, ADR-0008

**Purpose:** recruitment requests + applications reads, analytics, and request posting. **Ports:** 8080 (container) /
5202 (local). **DB:** illumin360_recruitment (schema `recruitment`). **Broker:** RabbitMQ (publishes `RecruitmentRequestPosted`).

**Data note:** the `recruitment_requests` and `applications` tables are pre-existing and externally **seeded**
(~19K requests / ~200K applications, a decade of history). They are mapped for query/write but **excluded from
migrations** (`ToTable(..., t => t.ExcludeFromMigrations())`) — only the MassTransit outbox tables (`InboxState`,
`OutboxState`, `OutboxMessage`) are migration-managed by the `InitialOutbox` migration. `RequestStatus` round-trips
to the seeded lowercase `status` values (`open`/`filled`/`closed`) via an explicit value converter.

## Endpoints
- `GET /v1/recruitment/requests?city=&status=&page=&pageSize=` (ILIKE city filter, case-insensitive status).
- `GET /v1/recruitment/stats` — funnel (by pipeline stage), hires/applications monthly trend, talent-type split,
  top cities, totals, avg match score.
- `GET /v1/recruitment/requests/{id}` · `GET /v1/recruitment/requests/{id}/applications` (highest match first).
- `POST /v1/recruitment/requests` (post a request → publishes `RecruitmentRequestPosted` via outbox).
- Via gateway: `/api/recruitment/**` → rewritten to `/v1/recruitment/**`.

## Deploy / rollback
- Deploy: `docker compose -f deploy/docker/docker-compose.yml -f deploy/observability/docker-compose.observability.yml \
  -f deploy/docker/docker-compose.apps.yml up -d --build recruitment-api` (local) / Helm chart (k8s).
- Migrations apply automatically at startup (`MigrateAsync`) and only ever create the outbox tables; the seeded
  domain tables are never touched. Migrations must be backward-compatible (expand/contract).
- Rollback: redeploy previous image tag. The outbox migration is additive and safe to leave in place.

## Probes
- `/health/live` process · `/health/ready` (DB `recruitment-db` + `masstransit-bus`) · `/health/startup` migrations applied.

## Common alerts → response
| Alert | Likely cause | First response |
| --- | --- | --- |
| High 5xx rate (RED) | DB down / bad deploy | Check `/health/ready`; inspect Loki logs by TraceId; roll back if deploy-correlated |
| p95 latency breach on `/stats` | Heavy aggregation / missing index | Check Tempo traces; review GROUP BY queries; consider a read replica / materialized rollup |
| Readiness failing | Postgres unreachable | Verify postgres container + connection string secret in Vault |
| Outbox growth | RabbitMQ unreachable | Check broker health; messages drain from `OutboxMessage` once the bus reconnects |

Telemetry: source `recruitment`. Gateway alias: `recruitment-api`.
