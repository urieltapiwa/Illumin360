# Runbook — Notifications Worker

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-05-30 · **Related ADRs:** ADR-0004, ADR-0007, ADR-0008

**Purpose:** consumes Candidates integration events and triggers onboarding notifications (welcome email,
matching-pipeline hand-off). **Ports:** 8080 (container) / 5301 (local). **DB:** none (stateless consumer;
future: `illumin360_notifications` for delivery logs). **Broker:** RabbitMQ.

## Consumes
| Event | Source exchange | Endpoint (queue) |
| --- | --- | --- |
| `Illumin360.Candidates.IntegrationEvents.CandidateRegistered` | `Illumin360.Candidates.IntegrationEvents:CandidateRegistered` | `candidate-registered` |

## Deploy / rollback / scale
- Deploy: `docker compose ... up -d --build notifications-worker` (local) / Helm chart (k8s).
- Rollback: redeploy previous image tag; consumers are idempotent, so reprocessing is safe.
- Scale: add replicas — the competing-consumers pattern on the `candidate-registered` queue spreads load.

## Probes
- `/health/live` process · `/health/ready` (+ `masstransit-bus`) · `/health/startup`.

## Common alerts → response
| Alert | Likely cause | First response |
| --- | --- | --- |
| Queue depth growing | Consumer down / slow / poisoned message | Check worker `/health/ready` + logs by TraceId; inspect DLQ `candidate-registered_error` |
| Messages dead-lettered | Repeated consume failures | Inspect `_error` queue payloads; fix handler; shovel/replay after fix |
| Bus not started | RabbitMQ unreachable | Verify rabbitmq container healthy + `ConnectionStrings__rabbitmq` |

Telemetry: source `notifications`. Verify the loop: POST a candidate to `:5201`, then
`docker logs illumin360-notifications-worker` shows the onboarding log line for that candidate id.
