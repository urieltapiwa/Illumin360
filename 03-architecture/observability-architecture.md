# Observability Architecture

> **Status:** Draft · **Owner:** Platform Architecture · **Last updated:** 2026-05-29 · **Related ADRs:** ADR-0001..0006

Charter Part 10. OTLP from every service/BFF/gateway/Keycloak → **Grafana Alloy** → Prometheus→Mimir (metrics),
Loki (logs), Tempo (traces) → Grafana. Single `AddProjectObservability(serviceName)` extension
(`src/BuildingBlocks/Observability`). Correlation via W3C trace context; Serilog attaches TraceId/SpanId.

## Custom telemetry catalogue (document every metric/span/check here)
| Name | Type | Unit | Labels | Meaning |
| --- | --- | --- | --- | --- |
| `candidates` (ActivitySource) | trace source | — | endpoint, status | Candidates service spans |
| `candidates.list.count` | counter (example) | items | city | Candidates returned per query |
| `candidates-db` | health check | — | ready, startup | PostgreSQL reachability |

Dashboards-as-code in `06-operations/dashboards/`; alerts in `06-operations/alerts/`; RED per endpoint, USE per resource.
