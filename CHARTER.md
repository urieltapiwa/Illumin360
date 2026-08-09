# AI Project Bootstrap Charter — Illumin360 (Completed)

**Status:** Active · **Owner:** Uriel Tapiwa Munjanga (Software Engineer & Architect) · **Last updated:** 2026-05-29
**Canonical living source:** Google Doc "AI_PROJECT_BOOTSTRAP_CHARTER" (enrich there; mirror material changes here).

This document is the binding engineering standard for Illumin360. Part 0 is the project brief; Parts 1–20 are the
standing instruction set the team and any assisting AI must follow for the entire engagement.

---

## PART 0 — PROJECT BRIEF

| Field | Value |
| --- | --- |
| **Project name** | Illumin360 — Talent Match & Recruitment Platform |
| **Short description** | AI-assisted platform that matches, ranks, and shortlists candidates for businesses, with self-service profiles for professionals and students. |
| **Goal** | A production-grade, compliant, observable multi-portal recruitment platform where Businesses find ranked candidates fast, Professionals/Students stay discoverable, and operations are auditable end-to-end. |
| **Vision** | The trusted talent layer for the region — AI matching, benchmarking/gamification, CSR student pipeline, and a marketplace that improves with RLHF feedback over time. |
| **Background / context** | Built by Illumin Investments CC (CC 2016/08234, VAT 07851437-015), trading as Illumin. Supersedes Technical Spec v3.6; current source of truth is the v3.7 amendment + `illumin360_master_migrations` schema. |
| **Primary domain** | HR tech / recruitment / talent marketplace. |
| **Target audiences** | **Client tier:** Professional (`job_seeker`), Student (`student`), Business (`employer`). **Staff tier:** Support (`support`). **Admin tier:** Administrator (`admin`). Five portals total. |
| **Compliance scope** | GDPR-equivalent data protection; local **Labour Act** compliance; **PCI-DSS** (payments); anti-discrimination controls; sensitive-filter governance; immutable audit trail. |
| **Deployment target** | Bare VM + Docker Compose (local/dev) mirrored to Kubernetes + Helm (staging/prod). |
| **Team size / cadence** | Lead engineer-driven, agile 2-week sprints; incremental delivery across Phases 1–8. |
| **Known constraints** | Open-source-only stack (operational footprint to be documented); preserve existing 70+ DB migrations (no schema rename — map client-facing ↔ technical terms at the app boundary); AI/ML SaaS dependencies (Anthropic, Google Vision) handled by exception ADR. |

### Client-facing ↔ technical terminology (binding)
| Client-facing | Technical identifier (DB/API) |
| --- | --- |
| Professional | `job_seeker` |
| Business | `employer` |
| Student | `student` (profile_type of job_seeker) |
| Administrator | `admin` |
| Support staff | `support` |

Map between surfaces at the boundary (controllers, view models, email templates). DB, migrations, and internal
OpenAPI identifiers keep `job_seeker` / `employer`.

### Microservice decomposition (bounded contexts)
`Identity`, `Candidates` (talent pool — **reference vertical slice**), `Employers`, `Recruitment` (matching/AI engine),
`Billing` (finance/subscriptions/payments), `Notifications`, `Support`, `Engagement` (social/referrals/badges/benchmarking),
`AiAssistant`. Object storage via MinIO; long-running workflows via Temporal. Database-per-service; Keycloak is the external IAM.

---

## PART 1 — AI OPERATING INSTRUCTIONS
Act as lead software engineer and solution architect; produce production-grade output; follow this charter as a contract.
**Principles:** (1) Open source only — permissive/compatible licences, recorded (Part 20). (2) Clean Architecture everywhere
(Domain → Application → Infrastructure → Presentation; dependencies point inward). (3) Microservices done deliberately — small,
independently deployable, own data, no shared DB. (4) Observability is not optional — instrument & health-check from day one.
(5) Document as you build. (6) Secure by default — threat-model first, no secrets in source, OWASP. (7) Ask, don't assume.
**Working method:** restate Part 0 → propose C4 L1 + service list → get approval → scaffold repo + doc templates → ADRs →
build one fully-instrumented vertical slice → expand. **Deliverables per service:** source, unit+integration tests, OpenAPI,
Dockerfile, health checks, OTel, Grafana dashboard + alerts, README, ADRs, CI pipeline.

## PART 2 — TECHNOLOGY STACK (pinned, all OSS; verify "latest stable" at scaffold time)
.NET 10 LTS / C# 14; .NET Aspire 13 (dev inner loop). ASP.NET Core Minimal APIs or controllers (consistent per repo);
gRPC internal, REST external. ASP.NET Core MVC frontends, each behind its own BFF. YARP gateway. Keycloak 26.6.x.
PostgreSQL 17 (DB-per-service) + EF Core 10; Redis; RabbitMQ (default) + MassTransit (outbox/sagas); MinIO; Temporal.
Polly resilience. OpenTelemetry → Grafana Alloy → LGTM (Loki/Grafana/Tempo/Mimir) + Prometheus; Serilog. Docker + Compose
(local), Kubernetes + Helm (deploy). GitHub Actions CI; Terraform IaC. HashiCorp Vault secrets. ClamAV file scanning.
xUnit + FluentAssertions + NSubstitute + Testcontainers + Pact + k6 + Stryker.NET. Trivy, OWASP ZAP, Dependabot/Renovate,
CycloneDX, Roslyn analyzers + StyleCop. Figma for design.

## PART 3 — REPOSITORY & SDLC FOLDER STRUCTURE
See the actual tree in this repository: governance/SDLC-as-code folders `00-governance` … `07-release`, plus `deploy/`,
`.github/`, and `src/` (BuildingBlocks, Gateway, Services, BFF, Apps). Per-service Clean Architecture layering:
`*.Domain` (no deps) → `*.Application` (depends on Domain) → `*.Infrastructure` (implements Application ports) →
`*.Api` (composition root: endpoints, DI, middleware, OpenAPI, health, OTel) + `tests/{Unit,Integration,Contract}`.
BFFs use identical layering; their Infrastructure holds typed HTTP/gRPC clients to domain services.

## PART 4 — SDLC DOCUMENT TEMPLATES
Skeletons with a header block (Status | Owner | Last updated | Related ADRs): Vision & Scope; BRD/FRD/NFR; User Story
(As a <role> I want <x> so that <y> + Gherkin AC + Definition of Ready); ADR; C4 (Context/Container/Component as code);
API contract (OpenAPI 3.1); Threat model (STRIDE); Test plan; Runbook; SLO/SLI sheet; Incident report (blameless);
Release plan & rollback strategy. (Illumin360's rich business docs live in the `0x_*` canonical folders.)

## PART 5 — ARCHITECTURE STANDARDS
Clean Architecture mandatory (Dependency Rule inward; Domain framework-agnostic). CQRS in Application (OSS/hand-rolled
mediator — ADR it). Ports & adapters. Domain events internal; integration events cross boundaries via broker + transactional
outbox. One bounded context per service; DB-per-service; idempotent handlers; backwards-compatible, versioned contracts.
Exactly one BFF per frontend; BFF handles aggregation, view-shaping, OIDC token relay (tokens stay server-side; browser gets
HttpOnly session cookie), and per-app caching.

## PART 6 — THE PORTALS (five, adapted from the charter's 3-app model)
| App | Audience | Keycloak client | Typical roles | Notes |
| --- | --- | --- | --- | --- |
| Professional.Web | Professionals (`job_seeker`) | professional-web (PKCE via BFF) | client.user | Self-service. Strict validation, rate limiting, least data exposure. |
| Student.Web | Students (`student`) | student-web | client.student | Free CSR profiles, verification, optional graduation upgrade. |
| Business.Web | Businesses (`employer`) | business-web | client.employer | Requests, search, reports, payments, internal portal. |
| Support.Web | Support staff (`support`) | support-web | support.l1, support.l2, support.lead | Triage, KYC, disputes, KB authoring. No financial-action authority. Audit-logged. |
| Admin.Web | Administrators (`admin`) | admin-web | admin.read, admin.write, admin.superuser | Config, user/role mgmt, pricing, feature flags. MFA enforced. |
BFF security pattern (no tokens in browser). AuthZ enforced at three layers: gateway (coarse), BFF (session/role),
service (fine-grained, resource-level).

## PART 7 — API & DESIGN STANDARDS
OpenAPI 3.1 = contract source of truth (design-first; publish to `04-design/api-contracts/`). REST: noun resources,
correct verbs/status, **RFC 9457 Problem Details** for all errors. Versioning via `/v1/`; never break a published contract.
Consistent pagination/filtering/sorting (cursor for large sets). Idempotency-Key on unsafe ops (dedupe via Redis).
Rate limiting at gateway + BFF. gRPC `.proto` versioned with the service. Propagate W3C `traceparent` end-to-end.

## PART 8 — IDENTITY & ACCESS (Keycloak)
Keycloak 26.6.x, Postgres-backed, HA-capable. Realm-per-environment; clients for gateway, each BFF, each service.
OIDC + OAuth 2.1; PKCE for browser flows via BFF; client-credentials for service-to-service. Access tokens 15-min,
rotating refresh 30-day, audience-scoped. Realm/client roles + groups + (where needed) UMA permissions mapped to app
authorization policies. Hardening: MFA for admin/support, passkeys/WebAuthn, brute-force detection, FAPI 2 for high-assurance.
Enable Keycloak OTel export. Provision realms/clients/roles as code (keycloak-config-cli or Terraform) — never click-ops in prod.

## PART 9 — SECURITY STANDARDS
Threat-model (STRIDE) per service in `03-architecture/security-architecture.md`. OWASP ASVS baseline; defend Top 10 + API Top 10.
Secrets in Vault only; gitleaks in CI. Supply chain: pin deps (central package mgmt), CycloneDX SBOM, Trivy, Renovate, signed
images. TLS everywhere, mTLS service-to-service. Encrypt DB volumes; field-level encryption for PII (`id_number`, `student_number`).
Validate at every boundary (FluentValidation); parameterised queries only. Pipeline gates: SAST (analyzers + CodeQL), SCA (Trivy),
DAST (ZAP) — fail on high/critical. Immutable audit logging for every Staff/Admin/Support mutation. Honour compliance scope.

## PART 10 — OBSERVABILITY (Grafana LGTM)
Every service/endpoint/job/outbound call emits metrics, logs, traces, correlated by trace id, visible in Grafana, alerted to SLOs.
Flow: .NET/Keycloak → OTLP → **Grafana Alloy** → Prometheus→Mimir (metrics), Loki (logs), Tempo (traces) → Grafana.
Single `AddProjectObservability()` extension in BuildingBlocks/Observability sets resource attributes, traces (ASP.NET Core,
HttpClient, EF Core, MassTransit, gRPC), metrics (+ custom business meters), and Serilog→OTel logs with trace/span ids.
Dashboards-as-code (RED per endpoint, USE per resource; service catalogue). SLOs in `06-operations/slo-sli.md`; multi-window
multi-burn-rate alerts linked to runbooks. Document every custom metric/span in `03-architecture/observability-architecture.md`.

## PART 11 — HEALTH CHECKS
Microsoft.Extensions.Diagnostics.HealthChecks + ecosystem checks (Postgres, Redis, RabbitMQ, Keycloak/OIDC, downstream HTTP, disk).
Three probes per service: `/health/live` (process), `/health/ready` (deps reachable), `/health/startup` (migrations applied, caches warm).
Map to k8s liveness/readiness/startup probes. Tag & filter checks. HealthChecks UI aggregate; surface health as Prometheus metrics.
Gateway and each BFF expose own + aggregate downstream health. Health results traced and logged.

## PART 12 — SERVICE COMMUNICATION & RESILIENCE
Sync: gRPC internal, REST external, via typed clients with Polly (timeout → retry w/ jitter → circuit breaker → fallback).
Async: events over RabbitMQ via MassTransit; transactional outbox; idempotent consumers; DLQs; sagas/state machines (or Temporal)
for long-running workflows. Shared event schemas in BuildingBlocks/Messaging or a registry; Pact contract tests. Backpressure &
graceful degradation: bulkheads, queue-depth alerts, feature flags to shed load.

## PART 13 — DATA & PERSISTENCE
PostgreSQL, DB-per-service, EF Core 10 code-first migrations applied at startup (gated by `/health/startup`). Redis for cache,
idempotency, rate limits, distributed locks. Patterns: outbox, optimistic concurrency, soft-delete where audit needs it, explicit
retention. Seed/reference data scripted & versioned. Backups + tested restore (documented in runbooks). MinIO for object storage.

## PART 14 — TESTING STRATEGY
Pyramid with enforced coverage gates (`05-quality/test-strategy.md`): Unit (Domain/Application, no I/O); Integration (real
Postgres/Redis/broker via Testcontainers); Contract (Pact); Component/API (WebApplicationFactory); E2E (critical journeys per portal);
Load/soak (k6 baselines per release); Mutation (Stryker.NET on core domain). All run in CI; PRs blocked on failure/coverage regression.

## PART 15 — CI/CD & IaC
Pipeline: restore+build (warnings-as-errors) → lint/format+analyzers → unit+integration+contract tests (+coverage gate) →
security (CodeQL SAST, Trivy SCA, gitleaks, CycloneDX SBOM) → build+scan image → deploy dev + DAST(ZAP)+smoke+health → staging
(auto checks + manual gate) → prod (canary/blue-green). Terraform/Pulumi modules per env; Helm for app deploy; GitOps optional.
Environment parity: local Compose/Aspire mirrors prod topology.

## PART 16 — DOCUMENTATION & COMMENTING
XML doc comments (`///`) on every public type/method/property (`<summary>`, `<param>`, `<returns>`, `<remarks>`, `<exception>`);
GenerateDocumentationFile on; missing-doc warnings as errors on public APIs. Summaries state intent/business meaning; name the
metric/span/check where telemetry is emitted. ADRs for every architectural decision; C4 kept current. READMEs at root and per
service. DocFX publishes docs in CI. Runbooks per service; changelog per release (SemVer + Keep-a-Changelog). Conventional Commits.

## PART 17 — DESIGN WORKFLOW (Figma)
Figma = design source of truth (low-fi → hi-fi → prototype per portal). Maintain a design system (tokens, components, states).
Export design tokens (Style Dictionary / Tokens Studio) → CSS variables consumed by MVC apps. Accessibility: WCAG 2.2 AA.
Each story links its Figma frame; UI PRs reference the frame.

## PART 18 — CODING STANDARDS
`.editorconfig` + Roslyn analyzers + StyleCop. `Directory.Build.props` enforces Nullable=enable, TreatWarningsAsErrors=true,
latest C#. Central Package Management (`Directory.Packages.props`). Naming, async-suffix, `Result<T>` over exceptions for expected
failures, guard clauses, immutability where practical. Trunk-based with short-lived branches (or GitFlow if release trains — ADR it).
PR template + CODEOWNERS + required review + green CI before merge.

## PART 19 — DEFINITION OF DONE
Done only when ALL hold: Clean Architecture + analyzers/format pass + warnings-as-errors clean; unit+integration+contract tests
green + coverage gate met; OpenAPI/.proto updated & backwards-compatible; `/health/{live,ready,startup}` cover new deps; OTel
traces/metrics/logs visible in Grafana + dashboard/alerts updated; AuthN/AuthZ enforced (gateway+BFF+service) + Keycloak roles
mapped; security scans pass (no high/critical) + secrets externalised + audit logging for mutations; XML docs + README/runbook/ADR/
changelog updated + DocFX builds; Dockerfile builds non-root image + Helm/Compose updated + CI green + deploys to dev with passing
smoke+health; accessibility checked for UI + Figma frame linked.

## PART 20 — GOVERNANCE, LICENSING & COMPLIANCE
OSS-only audit: every dependency licence in `00-governance/licenses/`; SBOM per build; flag non-permissive licences before adoption.
Data governance: classify data, retention/erasure per compliance scope, log PII access. Decision log: ADRs are canonical.
Cost/ops note: document operational footprint (who runs Keycloak/observability) so "free software" doesn't hide operational load.

---

## ENRICHMENT BACKLOG
- Message broker confirmed RabbitMQ (ADR-0004). Confirm Kafka only if event-streaming volume demands it.
- CI platform GitHub Actions + Terraform IaC — ADR-0005.
- Service mesh decision (none / Linkerd / Istio) for mTLS.
- Multi-tenancy model (single-tenant today).
- Feature-flag system (OSS: Unleash / Flagd).
- Background jobs/scheduling beyond Temporal (Quartz.NET) — licence-check.
- Search (OpenSearch) for candidate discovery if needed.
- DR RPO/RTO targets.
- i18n across the five portals.
- AI/ML SaaS exception (Anthropic Claude, Google Vision) — ADR-0006 documents the deviation from open-source-only.

*This charter is a living document. Update version pins, ADR links, and the enrichment backlog as the project evolves.*
