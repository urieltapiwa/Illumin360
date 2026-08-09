# Notifications Service (skeleton)

Email + multi-channel notification dispatch and logs (async via RabbitMQ).

> **Status:** scaffold only. Implement by mirroring `src/Services/Candidates/` — the reference
> vertical slice (Domain → Application → Infrastructure → Api + tests). Each service owns its own
> PostgreSQL database (`illumin360_notifications`), exposes
> `/health/{live,ready,startup}`, wires OTel via `AddProjectObservability("notifications")`,
> publishes its OpenAPI to `04-design/api-contracts/`, and ships a Dockerfile, Grafana dashboard, and runbook.

## Layers
- `Illumin360.Notifications.Domain` · `Illumin360.Notifications.Application` · `Illumin360.Notifications.Infrastructure` · `Illumin360.Notifications.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers) · ContractTests (Pact)
