# Changelog

All notable changes to this repository are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: [SemVer](https://semver.org/).

## [Unreleased]
### Added
- **Students service (third vertical slice):** `Illumin360.Students.{Domain,Contracts,Application,Infrastructure,Api}`
  exposing `GET /v1/students/me`, `GET /v1/students/{id}`, and `POST /v1/students`. Unlike Candidates/Recruitment
  (which map onto externally-seeded tables), the Students context **owns and migration-manages** all of its tables
  (`students`, `student_skills`, `student_learning`, `student_matches`, `student_pipeline`, `student_activity`) plus
  the MassTransit outbox — `InitialCreate` migration + `IDesignTimeDbContextFactory`, and a startup seeder that loads
  a demo cohort so the portal has live data out of the box. Publishes `StudentRegistered` via the transactional
  outbox (ADR-0007); `Illumin360.Students.Contracts` is the dependency-free event library (ADR-0008).
- **Gateway route** `/api/students/**` → rewritten to `/v1/students/**` with an active `/health/ready` check;
  `students-api` service (host port 5203) wired into `docker-compose.apps.yml` and the DB-per-service init list.
- **Student portal — live data:** `web/business-portal/src/Student.tsx` now reads `/api/students/me` via the BFF
  (snapshot fallback + LIVE chip), mirroring the Business dashboard's live-data pattern. KPIs (internship matches,
  modules done) derive from the real rows.
- **Dependency security bumps (repo-wide):** `System.Security.Cryptography.Xml` 10.0.6 → 10.0.10 and a pin of
  `Microsoft.OpenApi` to patched 2.11.0 (via `Microsoft.AspNetCore.OpenApi` 10.0.10) to clear newly-surfaced
  high-severity NuGet audit advisories that were breaking every service's clean build.
- **Business BFF (`Illumin360.Bff.Business`) — real server-side token handling:** an ASP.NET Core Backend-For-Frontend
  that runs the OIDC authorization-code + PKCE flow against Keycloak server-side, holds tokens in an encrypted
  HttpOnly cookie session, and YARP-reverse-proxies `/api/**` to the gateway with the user's access token attached
  (token relay) — the browser never sees a token. Endpoints `/bff/login`, `/bff/logout`, `/bff/user`; unauthenticated
  `/api` calls return 401. Verified host-local: health 200, `/bff/user` unauth, `/api` 401, `/bff/login` 302 to the
  Keycloak authorize endpoint with correct client_id + S256 PKCE. Confidential client `illumin360-business-bff`
  declared in `illumin360-realm.json`; runbook in `06-operations/runbooks/bff-business.md`.
- **SPA BFF mode:** `web/business-portal/src/auth.ts` switches to the BFF (`/bff/*`) when built with `VITE_USE_BFF=1`,
  otherwise keeps the existing public-client keycloak-js flow (non-breaking; the current demo is unaffected).
- **Shared `@illumin360/ui` design-system package:** extracted the reusable front-end design system — themed
  ECharts components + option builders, the CSS-variable theming engine (`THEMES`/`useTheme`/`applyTheme`),
  the language/theme switchers, the shared language list, and chart-input types — into
  `web/business-portal/packages/ui` (own `package.json` + barrel). The app consumes it as `@illumin360/ui`
  via a vite + tsconfig alias and Tailwind content glob. Production build (`tsc -b && vite build`) green;
  runtime behaviour verified identical to before extraction. Ready to hoist to an npm workspace when a 2nd app lands.
- **Production SPA container:** multi-stage Dockerfile (vite build → nginx static serve with SPA history fallback)
  + `.dockerignore`, built in BFF mode. `web` (nginx) and `bff-business` services wired into `docker-compose.apps.yml`
  (the BFF fronts the SPA same-origin); `docker compose config` validates and the SPA image serves (verified).
  Full BFF login + token relay verified end-to-end host-local against the shared Keycloak (demo user → cookie
  session → `/bff/user` → `/api` returns live data); the Docker OIDC path needs Keycloak hostname alignment (documented).
- **Recruitment service (second vertical slice):** `Illumin360.Recruitment.{Domain,Contracts,Application,Infrastructure,Api}`
  exposing `GET /v1/recruitment/requests` (city/status filter + paging), `/stats` (funnel, hires trend,
  matching, top cities), `/requests/{id}`, `/requests/{id}/applications`, and `POST /v1/recruitment/requests`.
  Maps the `RecruitmentRequest` aggregate + read-only `RecruitmentApplication` projection onto the existing,
  externally-seeded `recruitment_requests`/`applications` tables (a decade of history: ~19K requests / ~200K
  applications) via `ToTable(..., t => t.ExcludeFromMigrations())` — only the MassTransit outbox tables are
  migration-managed (`InitialOutbox`). Publishes `RecruitmentRequestPosted` via the transactional outbox (ADR-0007).
- **`Illumin360.Recruitment.Contracts`** — dependency-free integration-event library (`RecruitmentRequestPosted`) (ADR-0008).
- **Gateway route** `/api/recruitment/**` → rewritten to `/v1/recruitment/**` with an active `/health/ready` check.
- **Business portal — live recruitment analytics:** the hiring-funnel and hires-per-year charts and the
  hires-placed / fill-rate KPIs now read live from `/api/recruitment/stats` (snapshot JSON fallback + LIVE chips).
- **Frontend localisation — all five portals (EN + Afrikaans):** Professional, Student, Admin, and Support
  portals are now fully localised via `react-i18next` (Business + Login already were), each with its own
  per-portal locale module under `web/business-portal/src/locales/` merged into `i18n.ts`, plus the
  language + theme switchers in every header. 187 namespaced keys, verified to resolve in both languages.
- **Drill-down / cross-filter:** clicking a city in the Business dashboard's "Talent by city" chart
  cross-filters the live talent feed (via the Candidates API `?city=` ILIKE filter) with a clearable chip —
  a real, backend-powered interactive drill-down. The shared `Chart` component now accepts an `onClick` handler.
- **Candidates write slice:** `POST /v1/candidates` (register) and `GET /v1/candidates/{id}`, with
  `RegisterCandidateCommand` / `GetCandidateByIdQuery` handlers and RFC 9457 ProblemDetails mapping.
- **Transactional outbox** (MassTransit EF Core bus outbox) in Candidates — integration events are written
  to the outbox in the same transaction as the aggregate and delivered to RabbitMQ after commit (ADR-0007).
  Handlers publish via the `IIntegrationEventPublisher` port (MassTransit adapter in Infrastructure).
- **`Illumin360.Candidates.Contracts`** — shared, dependency-free integration-event library
  (`CandidateRegistered`); publisher and consumers bind to one contract (ADR-0008).
- **`Illumin360.Notifications.Worker`** — first downstream consumer (`IConsumer<CandidateRegistered>`,
  host port 5301), wired into Compose and the solution; runbook in `06-operations/runbooks/notifications.md`.
- **EF Core `InitialCreate` migration** for the Candidates schema (candidate table + MassTransit outbox
  tables) and an `IDesignTimeDbContextFactory`.
- Engineering scaffold generated from the AI Project Bootstrap Charter:
  - Repo + SDLC-as-code folder structure (Part 3).
  - Root governance/build files (README, CHARTER, LICENSE, editorconfig, central package management).
  - Docker Compose local environment: PostgreSQL 17, Redis, Keycloak 26.6, RabbitMQ, MinIO.
  - Grafana LGTM observability stack (Alloy, Prometheus, Loki, Tempo, Mimir, Grafana) as code.
  - .NET 10 solution: BuildingBlocks (SharedKernel, Observability, Web), YARP Gateway,
    Candidates microservice vertical slice, BFF + App skeletons.
  - Initial ADRs, C4 diagrams, CI/CD pipeline, and SDLC document templates.

### Changed
- Removed the Compose container `HEALTHCHECK` from the chiseled .NET services (candidates-api, recruitment-api,
  notifications-worker): the `CMD-SHELL` probe can't run without a shell, so they reported a false "unhealthy".
  Liveness/readiness remain exposed over HTTP (`/health/live`, `/health/ready`) for the gateway's active checks
  and Kubernetes probes. Recreated the running containers — `docker ps` now shows them healthy/clean.
- Front-end now passes a clean production build (`tsc -b && vite build`): resolved 12 strict-TypeScript
  errors in the portal chart builders (annotated ECharts option return types, narrowed `valueFormatter`/label
  formatter params, `as const` on shared axis/tooltip configs) and removed unused declarations.
- Candidates startup now applies EF Core migrations (`MigrateAsync`) instead of `EnsureCreated`.
- `.editorconfig` reconciles StyleCop/analyzer rules with the documented house style (and is now copied
  into the Docker build); EF-generated migrations are marked `generated_code`.

### Security
- Bumped OpenTelemetry to 1.15.3 (CVE-2025-27513 Api DoS; CVE-2026-42191 OTLP exporter path).
- Pinned `System.Security.Cryptography.Xml` to 10.0.6 (CVE-2026-33116, pulled transitively by EF Design).
