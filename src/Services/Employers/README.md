# Employers Service (skeleton)

Business (employer) profiles, badges, internal recruitment portal data.

> **Status:** scaffold only. Implement by mirroring `src/Services/Candidates/` — the reference
> vertical slice (Domain → Application → Infrastructure → Api + tests). Each service owns its own
> PostgreSQL database (`illumin360_employers`), exposes
> `/health/{live,ready,startup}`, wires OTel via `AddProjectObservability("employers")`,
> publishes its OpenAPI to `04-design/api-contracts/`, and ships a Dockerfile, Grafana dashboard, and runbook.

## Layers
- `Illumin360.Employers.Domain` · `Illumin360.Employers.Application` · `Illumin360.Employers.Infrastructure` · `Illumin360.Employers.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers) · ContractTests (Pact)
