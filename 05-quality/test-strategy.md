# Test Strategy

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-05-29 · **Related ADRs:** —

Charter Part 14 pyramid with enforced gates.

| Layer | Tooling | Target |
| --- | --- | --- |
| Unit (Domain/Application) | xUnit + FluentAssertions + NSubstitute | line coverage ≥ 80% on Domain/Application |
| Integration | Testcontainers (Postgres/Redis/RabbitMQ) | critical data paths |
| Contract | Pact (consumer-driven; BFF↔service) | all cross-service contracts |
| Component/API | WebApplicationFactory | all public endpoints |
| E2E | per-portal critical journeys | smoke + top journeys |
| Load/soak | k6 (baselines in `performance/`) | p95 < 300 ms @ target RPS |
| Mutation | Stryker.NET on core domain | score ≥ 60% (raise over time) |

All run in CI; PRs blocked on failure or coverage regression. Reference tests live in
`src/Services/Candidates/tests/`.
