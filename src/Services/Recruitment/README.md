# Recruitment Service (skeleton)

AI matching engine: recruitment requests, candidate matches, shortlists, auto-application matches, match feedback (RLHF signal).

> **Status:** scaffold only. Implement by mirroring `src/Services/Candidates/` — the reference
> vertical slice (Domain → Application → Infrastructure → Api + tests). Each service owns its own
> PostgreSQL database (`illumin360_recruitment`), exposes
> `/health/{live,ready,startup}`, wires OTel via `AddProjectObservability("recruitment")`,
> publishes its OpenAPI to `04-design/api-contracts/`, and ships a Dockerfile, Grafana dashboard, and runbook.

## Layers
- `Illumin360.Recruitment.Domain` · `Illumin360.Recruitment.Application` · `Illumin360.Recruitment.Infrastructure` · `Illumin360.Recruitment.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers) · ContractTests (Pact)
