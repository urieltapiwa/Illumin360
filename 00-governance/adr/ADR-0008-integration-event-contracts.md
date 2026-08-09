# ADR-0008: Integration-event contracts in a shared library

- **Status:** Accepted
- **Date:** 2026-05-30

## Context
MassTransit binds publisher and consumer by the message type's **namespace + name** (the RabbitMQ exchange
is `<Namespace>:<TypeName>`). A consumer in another service must reference the exact same type — but must
**not** take a dependency on the publisher's internals (its Application/Domain layers).

## Decision
Publish integration-event records from a dependency-free **contracts library** per bounded context, e.g.
`Illumin360.Candidates.Contracts` (namespace `Illumin360.Candidates.IntegrationEvents`). Both the publisher
(`Candidates.Application`) and consumers (`Notifications.Worker`) reference it. The **namespace is part of
the wire contract** and must remain stable across versions.

## Consequences
**Positive:** One source of truth for each contract; consumers avoid coupling to service internals; renaming
a type/namespace becomes a conscious, reviewable contract change rather than a silent break.
**Negative / trade-offs:** An extra project per context; cross-service version discipline needed when a
contract evolves (prefer additive changes).

## Alternatives considered
1. Duplicate the record in each service with a matching namespace — rejected: silent drift renames the
   exchange and breaks delivery with no compile error.
2. Reference the publisher's Application assembly — rejected: leaks internals and inverts dependencies.
