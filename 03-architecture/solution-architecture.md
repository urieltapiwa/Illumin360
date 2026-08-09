# Solution Architecture

> **Status:** Draft · **Owner:** Platform Architecture · **Last updated:** 2026-05-30 · **Related ADRs:** ADR-0001..0008

Clean Architecture per service and BFF (charter Part 5). CQRS in the Application layer (hand-rolled
`IQuery`/`IQueryHandler` + `ICommand`/`ICommandHandler` to avoid licence concerns — see
`src/.../Application/Abstractions/Cqrs.cs`). Edge: YARP gateway. IAM: Keycloak (ADR-0003). Sync: gRPC
internal / REST external with Polly. Async: RabbitMQ + MassTransit **transactional outbox** (ADR-0004/0007);
long-running workflows in Temporal. Object storage: MinIO. See `c4/context.md`, `c4/container.md`.

Integration-event contracts are published from per-context, dependency-free libraries
(`*.Contracts`, ADR-0008) referenced by both publishers and consumers, so the broker and cross-service
contracts never leak into a service's Domain/Application internals. Implemented end-to-end loop:
**Candidates** publishes `CandidateRegistered` (via the outbox) → **Notifications** worker consumes it.
