# Billing Service (skeleton)

Finance: subscriptions, pricing plans, payments, invoices, receipts, business subscriptions. PCI-DSS scope.

> **Status:** scaffold only. Implement by mirroring `src/Services/Candidates/` — the reference
> vertical slice (Domain → Application → Infrastructure → Api + tests). Each service owns its own
> PostgreSQL database (`illumin360_billing`), exposes
> `/health/{live,ready,startup}`, wires OTel via `AddProjectObservability("billing")`,
> publishes its OpenAPI to `04-design/api-contracts/`, and ships a Dockerfile, Grafana dashboard, and runbook.

## Layers
- `Illumin360.Billing.Domain` · `Illumin360.Billing.Application` · `Illumin360.Billing.Infrastructure` · `Illumin360.Billing.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers) · ContractTests (Pact)
