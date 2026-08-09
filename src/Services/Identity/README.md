# Identity Service (skeleton)

Profile shells for users/job_seekers/employers/support_staff. Authentication delegated to Keycloak; this service holds platform-side profile + role data only.

> **Status:** scaffold only. Implement by mirroring `src/Services/Candidates/` — the reference
> vertical slice (Domain → Application → Infrastructure → Api + tests). Each service owns its own
> PostgreSQL database (`illumin360_identity`), exposes
> `/health/{live,ready,startup}`, wires OTel via `AddProjectObservability("identity")`,
> publishes its OpenAPI to `04-design/api-contracts/`, and ships a Dockerfile, Grafana dashboard, and runbook.

## Layers
- `Illumin360.Identity.Domain` · `Illumin360.Identity.Application` · `Illumin360.Identity.Infrastructure` · `Illumin360.Identity.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers) · ContractTests (Pact)
