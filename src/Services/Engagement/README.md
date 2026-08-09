# Engagement Service (skeleton)

Social/community: referrals, insights, spotlight features, demand-feed cache, badges, benchmarking/leaderboards.

> **Status:** scaffold only. Implement by mirroring `src/Services/Candidates/` — the reference
> vertical slice (Domain → Application → Infrastructure → Api + tests). Each service owns its own
> PostgreSQL database (`illumin360_engagement`), exposes
> `/health/{live,ready,startup}`, wires OTel via `AddProjectObservability("engagement")`,
> publishes its OpenAPI to `04-design/api-contracts/`, and ships a Dockerfile, Grafana dashboard, and runbook.

## Layers
- `Illumin360.Engagement.Domain` · `Illumin360.Engagement.Application` · `Illumin360.Engagement.Infrastructure` · `Illumin360.Engagement.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers) · ContractTests (Pact)
