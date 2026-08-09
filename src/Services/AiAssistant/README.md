# AiAssistant Service (skeleton)

AI Platform Assistant: assistant conversations, AI processing log, escalation into Support.

> **Status:** scaffold only. Implement by mirroring `src/Services/Candidates/` — the reference
> vertical slice (Domain → Application → Infrastructure → Api + tests). Each service owns its own
> PostgreSQL database (`illumin360_aiassistant`), exposes
> `/health/{live,ready,startup}`, wires OTel via `AddProjectObservability("aiassistant")`,
> publishes its OpenAPI to `04-design/api-contracts/`, and ships a Dockerfile, Grafana dashboard, and runbook.

## Layers
- `Illumin360.AiAssistant.Domain` · `Illumin360.AiAssistant.Application` · `Illumin360.AiAssistant.Infrastructure` · `Illumin360.AiAssistant.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers) · ContractTests (Pact)
