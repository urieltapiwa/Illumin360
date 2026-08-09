# C4 Level 2 — Container

> **Status:** Draft · **Owner:** Platform Architecture · **Last updated:** 2026-05-30 · **Related ADRs:** ADR-0001..0008

```mermaid
flowchart TB
    subgraph Browsers
      P[Professional.Web]; ST[Student.Web]; B[Business.Web]; SU[Support.Web]; AD[Admin.Web]
    end
    subgraph BFFs
      PB[Professional.Bff]; STB[Student.Bff]; BB[Business.Bff]; SUB[Support.Bff]; ADB[Admin.Bff]
    end
    GW["YARP Gateway (edge, rate-limit, coarse authz)"]
    subgraph Services["Microservices (DB-per-service)"]
      ID[Identity]; CA[Candidates]; EM[Employers]; RC[Recruitment / AI match]
      BI[Billing]; NO[Notifications]; SP[Support]; EN[Engagement]; AI[AiAssistant]
    end
    KC[(Keycloak)]; PG[(PostgreSQL 17)]; RD[(Redis)]; MQ[(RabbitMQ)]; OBJ[(MinIO)]; TMP[(Temporal)]
    OTEL["Grafana Alloy -> LGTM + Prometheus"]

    P-->PB; ST-->STB; B-->BB; SU-->SUB; AD-->ADB
    PB & STB & BB & SUB & ADB --> GW
    GW --> ID & CA & EM & RC & BI & NO & SP & EN & AI
    BFFs -. OIDC .-> KC
    Services --> PG
    Services -. cache/locks .-> RD
    CA -. "events (outbox)" .-> MQ
    Services -. events .-> MQ
    MQ -. "CandidateRegistered" .-> NO
    RC & SP & CA -. files .-> OBJ
    RC & BI -. workflows .-> TMP
    Services & GW & KC -. OTLP .-> OTEL
```

Reference vertical slice implemented: **Candidates** (Domain/Application/Infrastructure/Api + tests),
publishing `CandidateRegistered` via the **transactional outbox** (ADR-0007) to the **Notifications**
worker — the first end-to-end publish→consume loop. Shared contracts live in `*.Contracts` (ADR-0008).

Second slice implemented: **Recruitment** — same Clean Architecture layering, serving live analytics
(funnel, hires trend, matching, top cities) over a decade of seeded requests/applications and publishing
`RecruitmentRequestPosted` via the outbox. Its domain tables are pre-existing/seeded and mapped with
`ExcludeFromMigrations`, so only the MassTransit outbox tables are migration-managed. The Business portal's
hiring-funnel and hires-per-year charts are wired live to `GET /api/recruitment/stats` through the gateway.
