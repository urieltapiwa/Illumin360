# ADR-0004: Message broker — RabbitMQ + MassTransit

- **Status:** Accepted
- **Date:** 2026-05-29

## Context
Async work (matching, transcription, CV processing, notifications, internal-portal close-and-process) needs a
broker. Charter Part 2 lists RabbitMQ (default) vs Kafka as a decision point; spec v3.7 §17.3 confirms RabbitMQ.

## Decision
**RabbitMQ** fronted by **MassTransit** (outbox, idempotent consumers, DLQs, sagas). Re-evaluate Kafka only if
event-streaming volume materially exceeds queue semantics. Long-running workflows use **Temporal**.

## Consequences
**Positive:** Mature, simple ops, strong .NET support via MassTransit; Temporal removes bespoke saga code.
**Negative:** Not optimised for high-throughput event streaming/replay (revisit with an ADR if needed).
