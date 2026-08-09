# ADR-0007: Transactional outbox for integration events

- **Status:** Accepted
- **Date:** 2026-05-30

## Context
Services must publish integration events (e.g. *candidate registered*) to RabbitMQ without a dual-write
inconsistency: the aggregate write and the event publish must be atomic, or a crash between them either
loses an event or emits one for an uncommitted change. Charter Part 5/13; ADR-0004 selected RabbitMQ +
MassTransit.

## Decision
Use the **MassTransit EF Core bus outbox** in each publishing service. Integration events are written to
`OutboxMessage` / `OutboxState` / `InboxState` tables in the **same `SaveChanges` transaction** as the
aggregate; a delivery service relays them to the broker only after commit. Implemented first in
**Candidates**: `AddEntityFrameworkOutbox<CandidatesDbContext>(o => { o.UsePostgres(); o.UseBusOutbox(); })`.
Handlers publish through an Application-layer port (`IIntegrationEventPublisher`) with a MassTransit adapter
in Infrastructure, keeping the broker out of the Application layer (ADR-0001 Clean Architecture).

## Consequences
**Positive:** Atomic persist + publish; at-least-once delivery; no lost/orphan events across a crash;
the broker stays out of Application.
**Negative / trade-offs:** Outbox tables + a delivery sweep per publishing service; consumers must be
**idempotent** (at-least-once semantics); events are delivered slightly after commit, not inline.

## Alternatives considered
1. Publish directly from the handler — rejected: dual-write race (DB commit vs broker publish can diverge).
2. CDC / Debezium log tailing — rejected: heavier infra, not warranted at current scale.
