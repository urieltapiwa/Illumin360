# Support Service (skeleton)

Support Portal backend: tickets, messages, attachments (ClamAV-scanned), knowledge articles, support audit logs.

> **Status:** scaffold only. Implement by mirroring `src/Services/Candidates/` — the reference
> vertical slice (Domain → Application → Infrastructure → Api + tests). Each service owns its own
> PostgreSQL database (`illumin360_support`), exposes
> `/health/{live,ready,startup}`, wires OTel via `AddProjectObservability("support")`,
> publishes its OpenAPI to `04-design/api-contracts/`, and ships a Dockerfile, Grafana dashboard, and runbook.

## Layers
- `Illumin360.Support.Domain` · `Illumin360.Support.Application` · `Illumin360.Support.Infrastructure` · `Illumin360.Support.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers) · ContractTests (Pact)
