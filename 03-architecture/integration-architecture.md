# Integration Architecture

> **Status:** Draft · **Owner:** Platform Architecture · **Last updated:** 2026-05-30 · **Related ADRs:** ADR-0001..0008

Sync contracts: REST (external, OpenAPI 3.1) + gRPC (internal). Async: integration events over RabbitMQ via
MassTransit with the transactional outbox; idempotent consumers; DLQs. W3C `traceparent` propagated end-to-end.
External integrations (ADR-0006): Anthropic Claude, Google Vision, payment gateway (HMAC-verified webhooks),
email/notification provider.

## Event flow (implemented — Candidates → Notifications)
1. A command handler raises a domain event and publishes an integration event via `IIntegrationEventPublisher`.
2. The MassTransit **EF Core bus outbox** writes it to the publishing service's `OutboxMessage` table inside
   the same `SaveChanges` transaction as the aggregate (ADR-0007); it is delivered to RabbitMQ after commit.
3. Consumers in other services bind to the exchange by the message **namespace + name** and process
   idempotently. Failures dead-letter to `<endpoint>_error`.

## Event catalogue
Integration-event contracts live in per-context, dependency-free libraries (ADR-0008), e.g.
`Illumin360.Candidates.Contracts`. The **namespace + type name** is the wire contract (the RabbitMQ exchange
is `<Namespace>:<TypeName>`); keep it stable.

| Event (type) | Publisher | Exchange | Consumer(s) → queue |
| --- | --- | --- | --- |
| `Illumin360.Candidates.IntegrationEvents.CandidateRegistered` | Candidates | `Illumin360.Candidates.IntegrationEvents:CandidateRegistered` | Notifications → `candidate-registered` |
