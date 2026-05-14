# Illumin360 Complete Technical Specification — v3.7 Amendment

| Document detail | Value |
|---|---|
| Document title | Illumin360 — Technical Specification v3.7 Amendment |
| Supersedes | v3.6 final (April 2026) |
| Issue date | 14 May 2026 |
| Author | Uriel Tapiwa Munjanga — Software Engineer & Architect |
| Classification | CONFIDENTIAL |
| Status | DRAFT — for review and sign-off before merging into the spec master |
| Scope | Adds Sections 32 through 37 (Support Portal, Auto-Application Engine, Benchmarking & Gamification, Standards & Architecture, Open-Source Stack, Business Subscription Model). Revises Sections 1.2, 13, 17.1, 17.3, 19 to reflect product reality and infrastructure decisions. |

> *This amendment formalises product and infrastructure decisions taken since v3.6 was issued. Once approved it is merged into the master spec, the cover page version is bumped to 3.7, and a v3.7 row is appended to the Section 19 change log. The corrected source then drives the v2.0 SDLC document population.*

---

## A. Terminology — client-facing vs technical

The platform adopts distinct terminology for client-facing artefacts versus internal technical/database identifiers. The intent is to use customer-friendly language in any user touchpoint while preserving the existing database schema's identifiers for backward compatibility.

| Client-facing term | Technical / database identifier | Used in |
|---|---|---|
| Professional | job_seeker (table, role, API path) | All UI strings, emails, marketing, user manuals, disclaimers where legally permissible, sales talking points |
| Business | employer (table, role, API path) | Same |
| Student | student (sub-type of job_seeker, profile_type field) | Same — no change |
| Administrator | admin | Same — no change |
| Support staff | support (new role per §17.1 amendment) | Support Portal users |

Application-layer code maps between the two surfaces at the boundary (controllers, view models, email templates). Database, migrations, and OpenAPI internal identifiers continue to use `job_seeker` / `employer` to avoid the cost of a schema rename across the 70 existing migrations.

## B. Section 1.2 — User types (revised)

Replaces the existing four-row table with the following:

| User type (client-facing) | Internal identifier | Role | Access level |
|---|---|---|---|
| Professional | job_seeker | Individuals subscribed to be discoverable for employment opportunities | Profile, CV, subscription, dashboard, payment history, badges, referral, public profile card |
| Student | student | Currently enrolled students — free CSR profile, pipeline candidates | Student profile, CV upload, modules, verification status, optional graduation upgrade |
| Business | employer | Organisations using the platform to identify, rank, and shortlist candidates | Requests, all search modes, auto-application surfacing, reports, payments, internal portal, employer profile and badges |
| Administrator | admin | Platform owner overseeing all operations, compliance, and analytics | Full system access — all users, all data, all functions |
| Support staff | support | Customer support overseeing tickets, KYC checks, dispute resolution, knowledge base authoring | Read access to most user data, write access to support entities, no financial-action authority |

Five distinct portal applications expose these roles — see §32.

## C. Section 13 — Database Schema (revised note)

Section 13.1 Domain Map adds a Support domain and aligns to the canonical `illumin360_master_migrations_v3.6.sql` migrations file.

| Domain | Tables (canonical) | Purpose |
|---|---|---|
| Identity & Access | Keycloak (external) + users, job_seekers, employers, support_staff (new) | Authentication delegated to Keycloak. Platform tables hold profile, billing, and role-specific data only. |
| Candidate Profile | (unchanged from v3.6) | — |
| Recruitment | (unchanged from v3.6) + auto_application_matches (new — see §33) | — |
| Finance | (unchanged) + business_subscriptions (new — see §37) | — |
| AI & Engagement | assistant_conversations, ai_processing_log, match_feedback, candidate_badges, employer_badges, referrals, insights, spotlight_features, demand_feed_cache | Already present in v3.4–3.6 migrations |
| Benchmarking | benchmark_snapshots, professional_rank_history, student_leaderboard_entries (new — see §34) | — |
| Support | support_tickets, support_messages, support_attachments, knowledge_articles (new — see §32) | — |

The canonical schema source remains `illumin360_master_migrations_v3.6.sql`, updated to v3.7 with the new tables.

## D. Section 17.1 — Security (revised)

Replaces the JWT and password-hashing rows with Keycloak as the IAM authority.

| Control | v3.7 |
|---|---|
| Authentication | **Keycloak** as the OIDC/OAuth2 authority. All portals authenticate through Keycloak. Tokens are Keycloak-issued — access tokens 15-minute expiry, refresh tokens 30-day expiry, configurable per realm. |
| Authorisation | Role-based access control via Keycloak realms and roles. Mapped to platform role identifiers (job_seeker, employer, admin, support). Enforced at the API gateway layer and re-checked in each microservice. |
| Password storage | Owned by Keycloak (argon2id by default). The platform never stores passwords. |
| MFA | Provided by Keycloak — TOTP, WebAuthn. Optional today, mandatory for `admin` and `support` realms. |
| Sensitive field encryption | id_number, student_number AES-256 at rest. Platform-managed via PostgreSQL pgcrypto. |
| HTTPS | Enforced platform-wide. HSTS headers. TLS 1.2+ minimum, 1.3 preferred. |
| Virus scanning | **ClamAV** for all uploaded files (CVs, videos, photos, logos). |
| Rate limiting | 100 requests per minute per authenticated user; 10 per minute for auth endpoints (Keycloak built-in plus API gateway enforcement). Public portal: 5 submissions per IP per hour. |
| Secret management | **HashiCorp Vault** (OSS) for all platform secrets — DB credentials, Anthropic API keys, payment gateway secrets, webhook signing keys. |
| Webhook verification | HMAC signature verified on every incoming payment webhook before processing. |

## E. Section 17.3 — Scalability and Reliability (revised)

| Control | v3.7 |
|---|---|
| Message queue | **RabbitMQ** — confirmed. Matching jobs, transcription, CV processing, notifications, internal portal close-and-process all run async via RabbitMQ queues. Idempotent consumers. |
| Cache and session | **Redis** — caching layer, rate-limit counters, distributed locks for founder grant serialisation (see §30). Keycloak session cache. |
| Object storage | **MinIO** (S3-compatible) — CVs, videos, photos, logos, generated reports. Abstraction layer permits migration to AWS S3 if required. |
| Workflow orchestration | **Temporal** (OSS) — long-running workflows including founder grant, internal portal close-and-process, subscription lifecycle, refund flows. Removes bespoke retry and saga logic. |
| Observability | **Grafana + Prometheus + Loki + Tempo** stack (the "LGTM" stack) with **OpenTelemetry** as the instrumentation standard. See §36. |
| Backups | Automated daily PostgreSQL backups with 30-day retention; weekly cross-region replication. MinIO bucket replication across availability zones. |
| Cron / scheduled jobs | Every-minute job for internal portal auto-close; daily job at 06:00 WAT for reminders, student graduation prompts, badge maintenance, demand-feed cache regeneration. Monthly job for Compliant Recruiter badge assessment. |

## F. Section 19 — Document Control (added row)

| Version | Date | Changes |
|---|---|---|
| 3.7 | 14 May 2026 | Added Section 32 Support Portal, Section 33 Auto-Application Engine, Section 34 Benchmarking and Gamification, Section 35 Standards and Architecture Alignment, Section 36 Open-Source Technology Stack, Section 37 Business Subscription Model. Revised Section 1.2 (added Support staff role; client-facing Professional/Business terminology), Section 13.1 (added Benchmarking and Support domains), Section 17.1 (Keycloak as IAM authority; ClamAV; Vault), Section 17.3 (RabbitMQ confirmed; Redis cache role; MinIO object storage; Temporal orchestration; LGTM observability). Existing 31 sections otherwise unchanged. |

---

# 32. Support Portal

The Support Portal is the fifth platform portal alongside Professional, Student, Business, and Administrator portals. It is the workspace for Illumin Investments customer-support staff handling tickets, identity verification queries, payment disputes, abuse reports, and knowledge-base authoring.

The Support Portal is intentionally separated from the Administrator Portal so that day-to-day customer support can be delegated to staff without granting full administrative authority over financial operations, compliance reviews, or platform configuration.

## 32.1 Capabilities

| Capability | Detail |
|---|---|
| Ticket triage | Inbound tickets from Professionals, Students, and Businesses arrive via email, in-product report buttons, and the AI Assistant escalation flow. Support staff classify, prioritise, and assign. |
| Ticket workspace | Per-ticket conversation thread, internal notes (not visible to customer), file attachments, status workflow (`open`, `awaiting_customer`, `awaiting_internal`, `resolved`, `closed`). |
| Read-only customer context | The ticket workspace shows the customer's profile summary, subscription status, recent payments, recent reports, badge progress — read-only. Support staff see the same context the AI Assistant sees, never another customer's data. |
| Knowledge base authoring | Markdown editor for FAQ articles. Published articles power the public FAQ and the AI Assistant's static knowledge layer. |
| Identity verification workflow | Student manual verification (per spec §4.2 Method 3) is owned by Support, not Admin. Business identity verification (KYC for new registrations) lives here too. |
| Abuse and moderation queue | Reported videos, flagged assistant conversations, complaints about employer behaviour. Support reviews, escalates to admin where required. |
| Refund triage | Initiates refund requests; final approval remains with admin per spec §16.2. |
| Dispute handling | Sensitive-filter complaints, employer review moderation, badge dispute requests. |
| Audit | Every action by Support staff is logged immutably with `support_user_id` and `correlation_id`. |

## 32.2 What Support staff cannot do

| Restriction | Rationale |
|---|---|
| No direct database write to financial tables | Prevents fraudulent refunds and credits. Support files a request; admin approves. |
| No password reset on behalf of user (Keycloak-managed) | Customers reset their own passwords via Keycloak. Support can trigger a reset email. |
| No access to another customer's private data unless within an active ticket from that customer | Strict scoping enforced at the API gateway |
| No override of compliance controls | Cannot approve a sensitive-filter request or override a Compliant Recruiter revocation |
| No access to admin configuration | Cannot change platform settings, pricing, notification templates |

## 32.3 Data model

| Table | Purpose |
|---|---|
| support_staff | Profile shell for Support users — display name, contact, role tier (`l1`, `l2`, `lead`). Identity owned by Keycloak. |
| support_tickets | One row per ticket — customer_user_id, subject, category, priority, status, assigned_to, created_at, resolved_at, sla_due_at |
| support_messages | Thread of messages on a ticket — sender (customer or staff), body, internal_note (boolean), attachments[] |
| support_attachments | File metadata for attachments (virus-scanned via ClamAV before storage) |
| knowledge_articles | Markdown articles — title, slug, body, category, status, published_at, view_count |
| support_audit_logs | Every action by Support staff with full metadata |

## 32.4 API and UI

Support Portal API endpoints under `/support/...` namespace, authenticated via Keycloak with `support` realm role. SLA timers, ticket lists, queues, knowledge-base editor.

UI is a desktop-oriented React application — same component library as the other portals but with denser, multi-pane layouts suited to triage work.

## 32.5 Integration with AI Platform Assistant

The AI Platform Assistant (§29) escalate button creates a `support_tickets` row with the conversation transcript and customer context snapshot attached. Support sees the full context at first response — no re-asking the customer what their issue is. This is the primary funnel into the support queue and the assistant's measured failure mode (filter_triggered_count, repeated reformulations) is a queue signal.

## 32.6 Phase and acceptance

Initial Support Portal scope is Phase 5 — concurrent with social features going live. Pre-launch operations rely on the Admin Portal alone; once user volume grows the Support Portal becomes operational.

Acceptance criteria — Phase 5 release:
1. Support staff authenticate through Keycloak `support` realm role
2. Inbound tickets land in the workspace from at least three channels (email, in-product report button, AI Assistant escalation)
3. Knowledge articles published in the Support Portal appear in the public FAQ and feed the assistant's static knowledge layer within 60 seconds
4. Student manual verification workflow operates end-to-end including admin sign-off where required
5. All Support actions are recorded in `support_audit_logs` and cannot be deleted by Support staff

---

# 33. Auto-Application Engine

The Auto-Application Engine is the defining feature of Illumin360 — the mechanism that surfaces top-tier Professionals and Students to a Business's recruitment request **without those candidates having to apply**. It implements the platform's core promise: no missed opportunities on either side of the market.

This engine is **distinct from** the four employer-initiated search modes in spec §5.1. Those modes describe employer pull behaviour — a Business creates a request, the matching engine ranks candidates against it. The Auto-Application Engine adds a complementary push behaviour: when a Business creates any request, the engine automatically populates the candidate pool from active Professional and Student subscribers whose profiles match the request criteria — there is no separate application step required from the candidate.

## 33.1 Core principle

> Every active Professional and Student profile is, by default, a continuous standing application against every recruitment request that fits their stated profile and preferences. The candidate does not click "apply"; the platform represents their availability through the active subscription itself.

This principle reframes what the platform sells:
- Professionals subscribe to be **passively discoverable**, not to actively apply
- Students subscribe to be **scouted**, not to actively apply
- Businesses receive **pre-curated, ranked candidate pools** for any request — not application piles to triage

## 33.2 Differences from existing search modes

| Aspect | Search Modes (§5.1) | Auto-Application Engine |
|---|---|---|
| Triggered by | Business clicks "Create request" | Same trigger — but the engine runs as part of every request creation |
| Candidate inclusion | Active subscribed candidates matching hard filters | Same set; the engine formalises that no application action is required |
| Output | Ranked shortlist for the report | Same shortlist plus an audit record per candidate of why they were considered |
| Candidate awareness | Candidate may be notified after the request is unlocked, depending on preferences | Candidate notification is governed by preferences and tier (see §33.5) |
| Billing | Per spec §2.3 | Unchanged |

The Auto-Application Engine is largely a renaming and formalisation of behaviour that the v3.6 matching engine already implements — but elevating it to a named platform feature has three real benefits:
1. Clear customer-facing positioning (no apply button required)
2. Explicit consent flow at candidate registration (the candidate consents to be auto-considered)
3. Auditable record per candidate per request of why they were surfaced — important for legal defensibility

## 33.3 Candidate consent

At Professional and Student registration, the platform consent flow (D-05, D-06) is amended to make the auto-application principle explicit:

> *"By creating an Illumin360 account, I agree to be automatically considered for recruitment requests on the Illumin360 platform that match my profile, qualifications, skills, and location preferences. I do not need to apply to individual postings — my active profile is my standing application across the platform. I can opt out of being considered for specific industries, employers, or request types in my account settings."*

The candidate's account-settings page exposes:
- **Opt-out by industry** — e.g., "Do not consider me for mining-sector requests"
- **Opt-out by employer** — block specific Businesses from seeing my profile
- **Opt-out by request type** — e.g., "Do not consider me for graduate trainee programmes"
- **Pause auto-application** — temporary pause without account closure

## 33.4 Engine flow

1. Business creates a recruitment request and submits (compliance declaration ticked per D-03)
2. The matching engine runs Pass 1 (hard filters) and Pass 2 (weighted scoring) against the full pool of active candidates whose profiles fit the criteria — this is the existing behaviour
3. The Auto-Application Engine layer writes a record to `auto_application_matches` for every candidate considered (not only those shortlisted) with the metadata of why they were included or excluded
4. Shortlisted candidates appear on the Business's report exactly as in v3.6
5. Optionally — at the Business's choice and the candidate's preference — surfaced candidates may receive a passive notification that they were matched and considered. No action required from the candidate.

## 33.5 Candidate notification tiers

| Tier | Notification |
|---|---|
| Silent (default for free / standard subscriptions) | Candidate is not told they were considered. The match exists in `auto_application_matches` for audit but no email is sent. |
| Notify on shortlist | Candidate is told their profile appeared in a shortlist report. No employer name disclosed until employer unlocks the candidate. |
| Notify on unlock | Candidate is told a Business has unlocked their profile. Employer name and reason disclosed at this stage. |

Tier is a candidate preference. Default is silent for privacy. Notify-on-shortlist or notify-on-unlock can be enabled to give Professionals visibility on their market activity.

## 33.6 Data model

| Table | Purpose |
|---|---|
| auto_application_matches | One row per (request_id, candidate_id) pair where the engine considered the candidate — whether shortlisted or not. Records: per-factor scores, hard-filter result, inclusion reason, exclusion reason if not shortlisted, notification tier applied, notification sent timestamp. |
| candidate_auto_preferences | Per-candidate opt-out settings — industries, employers, request types, paused flag, notification tier. |

`auto_application_matches` is the audit ledger that makes the engine defensible — every candidate considered has a documented score and reasoning. This is the data source for any subsequent dispute ("why was I not shortlisted?") and for the platform-level analytics in the Admin dashboard.

## 33.7 Reporting impact

The shortlist report (spec §10) gains an additional methodology line:

> *"This shortlist was generated automatically from the Illumin360 platform pool. Candidates did not apply to this specific request — they are continuously discoverable subject to their profile criteria and consent. See the Methodology Disclosure (Section 5 of this report) for the criteria applied."*

This makes the auto-application principle visible in every report — supporting the platform's value proposition and giving the recipient context for the candidates they're considering.

## 33.8 Phase

Phase 1 — the Auto-Application Engine is a formalisation and rebranding of behaviour already in scope for Phase 1 (Core Talent Pool). No new technical phase introduced; the consent text and `auto_application_matches` table land in Phase 1.

## 33.9 Acceptance criteria

1. Every recruitment request submission writes one `auto_application_matches` row per candidate considered, regardless of whether they were shortlisted
2. Candidate opt-out preferences (industry, employer, request type, paused) are honoured before any consideration takes place
3. Candidate consent flow at registration includes the auto-application principle explicitly
4. The shortlist report includes the auto-application methodology line
5. Candidate notification tier preference is honoured — no notifications sent in silent mode

---

# 34. Benchmarking and Gamification

This section formalises platform features that let each customer segment understand its position relative to the market:
- **Businesses** benchmark their internal workforce against industry distributions
- **Professionals** benchmark themselves against peers in their field and against role requirements they target
- **Students** rank themselves against other students nationally and within their institution, competing for graduate programmes

## 34.1 Business benchmarking — workforce vs industry

A subscribed Business can upload an anonymised snapshot of its workforce (role, qualification, NQF level, years of experience, skills, certifications) and receive a benchmarking report comparing the workforce against:
- Industry distribution (constructed from the platform's anonymised Professional pool)
- Top-performer profile composition (Professionals shortlisted ≥5 times)
- Skills gap analysis — which skills appear in market shortlists that the workforce does not currently hold

This addresses the explicit Business need: *"Why are we continuously being beaten?"*

### 34.1.1 Workflow

1. Business uploads a CSV of its workforce (no personally identifiable information required — role + qualification + skills + experience suffice)
2. The engine computes distributional comparisons against the platform's anonymised pool
3. A Workforce Benchmarking Report is generated in the same PDF/Word format as shortlist reports
4. Quarterly refresh option — workforce snapshots over time show whether the Business is closing or widening the gap

### 34.1.2 Pricing

| Service | Price |
|---|---|
| Workforce Benchmarking Report — one-off | NAD 3,500.00 base + VAT |
| Quarterly Benchmarking Subscription (4 reports/year) | NAD 12,000.00 base + VAT (annual) |

## 34.2 Professional benchmarking and gamification

Each Professional sees a personal dashboard widget — *"Where I stand"* — comparing them against the wider Professional pool on:
- Profile completion percentile
- Skill breadth and depth percentile
- Shortlist appearance rank (against Professionals in the same role category)
- Qualifications-vs-target-role gap

Gamification mechanics built on top:

| Mechanic | Detail |
|---|---|
| Skill quests | Suggested next skills to add to the profile to move up a percentile. Sourced from the gap analysis logic (spec §22.2) generalised to "what skill would improve my matching against requests in my field." |
| Certification suggestions | Suggested certifications most often present in shortlisted Professionals' profiles for the same role category. |
| Training partnerships | Curated training/certification programmes with platform partners. Booked through the platform; completion reflected on the profile automatically. |
| Leaderboard (opt-in) | Anonymous public leaderboard by role category — "Top 100 marketing professionals on Illumin360". Opt-in by Professional. |
| Badge progression | Existing badge system (§26, ILLM-03-019) — visible progression toward earnable badges. |

## 34.3 Student benchmarking and competition for graduate programmes

Students see comparable dashboards:
- Rank against other students at their institution
- Rank against other students nationally in their programme
- Eligibility for graduate trainee programme searches in their field
- Skills/modules gap vs successful graduates from prior cohorts

Competitive layer:

| Mechanic | Detail |
|---|---|
| Graduate Trainee Programme leaderboard | When a Business creates a Graduate Trainee Programme request (§4.3 Rule 3), the platform displays a notice to students whose profiles match the criteria. Students can opt to "Apply for consideration" — but per the Auto-Application principle (§33), they are already in consideration; the explicit action is for engagement only. |
| Monthly Top Student showcase | Top-ranked students per institution per month featured in the Graduate Spotlight (§21 F5). |
| Skill challenges | Curated learning paths sponsored by Businesses — completion adds to profile and signals to that sponsoring Business. |

## 34.4 Data model

| Table | Purpose |
|---|---|
| benchmark_snapshots | Business workforce snapshots — one row per Business per upload. Contains the anonymised composition. |
| benchmark_reports | Generated benchmarking reports — links back to the snapshot. |
| professional_rank_history | Per-Professional per-month rank snapshots — for the personal dashboard widget and historical trends. |
| student_leaderboard_entries | Per-Student per-period ranking — institution, programme, national. |
| skill_quest_assignments | Per-Professional or per-Student suggested skill quests — accepted, completed, dismissed. |

## 34.5 Privacy

All comparison data is built from anonymised aggregates. No comparison view ever names another individual without their consent. Leaderboards are opt-in and may use a display handle rather than the Professional's name. Business workforce snapshots are not shared back to the Professional pool — workforce data flows in, comparison flows out.

## 34.6 Phase

| Sub-feature | Phase |
|---|---|
| Professional personal dashboard widget | Phase 5 |
| Student leaderboard within institution | Phase 5 |
| Skill quests and suggestions | Phase 6 |
| Business Workforce Benchmarking Report | Phase 6 |
| Graduate Programme leaderboard | Phase 5 |

## 34.7 Acceptance criteria

1. Professional dashboard widget renders correctly with current rank against peers in the same role category
2. Student leaderboard updates daily and respects institution scope filters
3. Workforce Benchmarking Report renders within 60 seconds of CSV upload
4. No personally identifiable comparison data is exposed without explicit consent
5. Leaderboard entries respect the opt-in flag — absent by default

---

# 35. Standards and Architecture Alignment

The platform aligns with the following standards, frameworks, and architectural principles. Compliance is asserted in the SDLC document set and verified during the relevant review phases.

| Reference | Where applied |
|---|---|
| **ISO/IEC 12207** series — Software lifecycle processes | The SDLC document set itself (folders 01–14) follows 12207 activity definitions. Each phase's deliverables map to a 12207 process. |
| **ISO/IEC 81346** series — Reference designation system | Platform component IDs (ILLM-XX-YYY pattern) follow a structured reference designation. Functional and product-aspect identifiers used consistently. |
| **ISO 9241** series — Ergonomics of human-system interaction | UI/UX Design (ILLM-03-006). Particularly 9241-110 (dialogue principles) and 9241-210 (human-centred design). Accessibility audited against WCAG 2.1 AA. |
| **ISO/IEC 27001** series — Information Security Management | Security Design (ILLM-03-007), Compliance pack (folder 09), audit trail (§15.3). Designed to support eventual ISO 27001 certification. Control mapping appears in Security Design. |
| **OpenAPI** 3.x | API Design (ILLM-03-005) and API Documentation (ILLM-12-002). All REST endpoints described in a versioned OpenAPI specification file. Specification is the contract — code and client SDKs generated from it. |
| **Domain-Driven Design (DDD)** | Architecture Diagrams (ILLM-03-001). Bounded contexts: Identity (Keycloak-managed), Professional Profile, Business, Recruitment, Matching, Reporting, Payments, Notifications, AI Engagement, Social & Community, Benchmarking, Support. Each owns its data and exposes APIs. Ubiquitous language glossary maintained alongside. |
| **Clean Architecture** | Coding Standards (ILLM-07-001). Each microservice organised in Domain / Application / Infrastructure layers with dependencies pointing inward. Domain layer is framework-independent. |
| **Microservices** | Architecture aligned to DDD bounded contexts. Independent deployment, independent data ownership, async communication via RabbitMQ where appropriate. API gateway routes external traffic. |
| **REST** | Public APIs follow REST conventions — resource URIs, standard verbs, HTTP status codes, content negotiation, statelessness. RPC-style endpoints only where REST is a poor fit (webhooks, streaming). |
| **Microsoft .NET Naming Guidelines** | Coding Standards (ILLM-07-001). PascalCase for public types and members, camelCase for parameters and local variables, prefix `I` for interfaces. Implies .NET as the primary platform language. *To be explicitly confirmed in Technology Stack (ILLM-03-002).* |
| **WCAG 2.1 Level AA** | UI/UX Design and the Accessibility Review pass on every release. |
| **PCI-DSS SAQ A** | Already in spec §11.1 — payment hosted-page model keeps card data off-platform. |

## 35.1 Bounded-context to microservice map

The platform is structured into the following services. Each owns its bounded context and its data. Inter-service communication is async via RabbitMQ for events and sync via REST (through the API gateway) for queries.

| Service | Bounded context | Data store | Key external integrations |
|---|---|---|---|
| identity-svc | Keycloak adapter — user profile shell | PostgreSQL (users, profile_shell) | Keycloak |
| professional-svc | Professional/Student profile, CV, skills, languages | PostgreSQL + MinIO (CV/photo/video) | ClamAV (scan), Apache Tika (parse), Anthropic API (analyse) |
| business-svc | Business profile, billing context, internal portal config | PostgreSQL + MinIO (logos) | — |
| recruitment-svc | Recruitment requests, compliance justifications | PostgreSQL | — |
| matching-svc | Matching engine — scoring, ranking, auto-application | PostgreSQL + pgvector | Anthropic API (justification) |
| reporting-svc | PDF and Word report generation | MinIO (output) | WeasyPrint, python-docx |
| payment-svc | Payment initiation, webhook handling, invoice issuance | PostgreSQL | Payment gateway (TBD) |
| notification-svc | Email, in-product notifications | PostgreSQL + RabbitMQ | Email provider |
| ai-assistant-svc | Platform Assistant — session, context, escalation | PostgreSQL | Anthropic API |
| engagement-svc | Badges, referrals, social features, demand feed, spotlight | PostgreSQL | — |
| benchmarking-svc | Workforce snapshots, Professional and Student rankings | PostgreSQL | — |
| support-svc | Tickets, knowledge base, audit | PostgreSQL + MinIO | — |
| admin-svc | Admin dashboard, reports, audit views | PostgreSQL (read-mostly) | — |

The API gateway (Kong or Traefik) terminates TLS, validates Keycloak tokens, applies rate limits, and routes to the appropriate service.

---

# 36. Open-Source Technology Stack

The platform is built on a fully open-source runtime stack. The only non-OSS dependencies are the third-party AI services (§28 — Anthropic Claude Sonnet 4.6, Google Cloud Vision OCR) and the chosen payment gateway (§11).

## 36.1 Confirmed stack

| Domain | Tool | Role |
|---|---|---|
| Identity & Access Management | **Keycloak** | OIDC/OAuth2 authority, SSO, MFA, federation, social login. The single source of authentication for all five portals. |
| Observability — visualisation | **Grafana OSS** | Dashboards and alerting for metrics, logs, and traces |
| Observability — metrics | **Prometheus** | Time-series metrics scraping |
| Observability — logs | **Loki** | Log aggregation |
| Observability — traces | **Tempo** | Distributed tracing across microservices |
| Instrumentation | **OpenTelemetry** | Vendor-neutral instrumentation feeding Prometheus, Loki, Tempo |
| Message queue | **RabbitMQ** | Async job queue for matching, transcription, CV processing, notifications, portal close-and-process |
| Cache & session | **Redis** | Caching, rate-limit counters, Keycloak session cache, distributed locks |
| Object storage | **MinIO** | S3-compatible storage for CVs, videos, photos, logos, reports |
| Relational database | **PostgreSQL** 15+ | Primary data store across all services |
| Vector search | **pgvector** | Semantic similarity (PostgreSQL extension) — no separate vector DB required |
| Container runtime | **Docker** | Service containerisation |
| Orchestration | **Kubernetes** (or K3s for lighter footprint) | Microservices orchestration aligned to DDD contexts |
| GitOps deployment | **Argo CD** | Declarative deployment to Kubernetes |
| API gateway | **Kong** (OSS) or **Traefik** | External-traffic routing, OIDC validation, rate limiting |
| Reverse proxy | **NGINX** | TLS termination, static asset serving |
| Secrets | **HashiCorp Vault** (OSS) | DB credentials, API keys, webhook signing keys |
| Virus scanning | **ClamAV** | Required by §17.1 — uploaded files |
| Document parsing | **Apache Tika** | Text extraction from PDF/DOCX before AI analysis |
| OCR fallback | **Tesseract OCR** | Local fallback when Google Cloud Vision is unavailable |
| Workflow orchestration | **Temporal** (OSS) | Long-running workflows — founder grant, portal close, subscription lifecycle |
| PDF generation | **WeasyPrint** | Server-side HTML-to-PDF for shortlist reports (already in §10.2) |
| Word generation | **python-docx + docxtpl** | Already in §10.2 |
| Load testing | **k6** (Grafana Labs) | Pairs with the Grafana observability stack |
| E2E browser testing | **Playwright** | All five portals + PWA install |
| API testing | **Hoppscotch** | OSS Postman alternative |
| API documentation rendering | **Swagger UI** / **Redoc** | Renders the OpenAPI 3.x spec for ILLM-12-002 |
| Marketing automation | **Mautic** | Referral programme funnels, Talent Report email gating, Insights distribution |
| Self-hosted AI fallback (optional) | **Ollama** | Optional Phase 8 — local model fallback for the AI Assistant during vendor outages |
| Frontend | **TailwindCSS** (utility-first CSS) + framework TBD | Aligned with the existing component library which uses Tailwind conventions |

## 36.2 Application language

The standards list in §35 cites Microsoft .NET Naming Guidelines, implying **.NET (C# / ASP.NET Core)** as the primary application language. Subject to explicit confirmation in the Technology Stack document (ILLM-03-002). Entity Framework Core or Dapper as the ORM. Polly for resilience. Serilog for logging (feeds Loki via OpenTelemetry).

## 36.3 Container security

Container images are built in CI with **Trivy** scanning. Production images run as non-root, with read-only root filesystems where possible, in Kubernetes pods with network policies enforcing east-west traffic restrictions.

## 36.4 Branding policy compatibility

This stack is fully internal-only per §31. None of the tools above are mentioned in client-facing artefacts. The platform brands as Illumin360 throughout regardless of which underlying tools power it.

---

# 37. Business Subscription Model

This section reconciles the v3.6 spec's pay-per-request employer model with the product reality that **all three customer segments (Professional, Student, Business) are subscription-based**. The v3.7 model is hybrid: Businesses subscribe to a tier of platform access AND pay per-request fees for shortlist reports and candidate unlocks. The subscription provides predictable monthly value; the per-request fees preserve usage-based revenue alignment.

## 37.1 Tier structure

| Tier | Monthly base (excl. VAT) | Per-request discount | Included monthly allowance | Other benefits |
|---|---|---|---|---|
| Free | NAD 0 | None — full price applies | None | Limited candidate preview, can create requests but cannot unlock without payment. Used as the on-ramp. |
| Starter | NAD 1,500 / month | 10% off shortlist report unlock | 1 free shortlist report unlock per month | Branded Business profile, employer logo on reports, daily demand feed |
| Growth | NAD 3,500 / month | 25% off shortlist report unlock; 10% off candidate unlocks | 3 free shortlist reports per month | All Starter + Compliant Recruiter eligibility, monthly workforce snapshot benchmarking (§34.1) |
| Enterprise | NAD 10,000 / month | 40% off shortlist report unlock; 25% off candidate unlocks; 25% off internal recruitment | 10 free shortlist reports per month, unlimited internal portals | All Growth + dedicated Support tier, quarterly benchmarking subscription, priority assistant queue, custom matching weight presets |

Prices indicative; finalise during commercial review.

## 37.2 Founder Programme interaction

Founding Partners (first 50 employers per §30) receive **the Growth tier permanently at zero monthly cost** as their Founding Partner benefit. They continue to pay per-request fees at the discounted Growth rates. This adjusts the §30.2 Founder benefit from "no monthly billing on Business accounts ever" (which was vacuous under pure pay-per-request) to "permanent Growth-tier subscription value" — a meaningful and quantifiable benefit.

## 37.3 Data model

| Table | Purpose |
|---|---|
| business_subscriptions | One row per Business per active subscription — tier, start_date, end_date, status, monthly_renewal_amount, free_allowance_remaining_this_period |
| business_subscription_history | Tier changes, upgrades, downgrades, cancellations |

The existing `subscriptions` table covers Professional/Student. Business subscriptions are split into the new table because their billing cadence and allowance mechanics differ (monthly recurring + per-request allowances, vs Professional fixed-term subscriptions).

## 37.4 Free allowance mechanics

The included monthly shortlist reports are tracked on `business_subscriptions.free_allowance_remaining_this_period`. When a Business unlocks a report:
1. If `free_allowance_remaining_this_period > 0`, decrement and unlock at zero cost
2. Else, charge the per-request discounted price for the tier

Allowance resets on the subscription anniversary each month. Unused allowance does not roll over.

## 37.5 Migration

| Existing state | v3.7 transition |
|---|---|
| Existing pay-per-request Businesses (pre-launch) | Migrated to Free tier — no monthly fee, no allowance. Continue paying full per-request fees. |
| Founding Partner Businesses | Migrated to Growth tier permanently at zero cost |
| New Business registrations after v3.7 launch | Default Free tier; upsold to Starter/Growth in onboarding |

## 37.6 Phase

The subscription tier infrastructure lands in Phase 2 (payments) — the same phase as the existing per-request payment flow. Free-allowance mechanics integrate into the same payment service.

## 37.7 Acceptance criteria

1. A Business can subscribe to a tier and see the tier reflected in their dashboard
2. Free monthly allowance decrements correctly on each unlock; resets on anniversary
3. Per-request discount applies correctly to the tier when no allowance remains
4. Founding Partners receive Growth tier permanently at zero cost — monthly renewal cron skips them
5. Tier downgrades take effect at the next renewal, not mid-period
6. All tier changes are recorded in `business_subscription_history` and cannot be deleted

---

# Closing notes

This amendment introduces six new sections (32–37) and revises four existing sections (1.2, 13.1, 17.1, 17.3) plus Section 19 change history. Once approved, the amendment is merged into the spec master, the cover-page version bumps to 3.7 (the cover-page version inconsistencies from the v3.6 source corrections register should be resolved in the same pass), and the v2.0 SDLC document population proceeds against the corrected source.

The detailed-design documents previously produced (ILLM-03-011 through ILLM-03-021) are not invalidated by this amendment, but they require a terminology and cross-reference pass to align with: Professional/Business client-facing language, Support Portal where relevant, Auto-Application Engine where relevant, and the new OSS stack references.

## Sign-off

| Role | Name | Action |
|---|---|---|
| Software Engineer & Architect | Uriel Tapiwa Munjanga | Authored amendment |
| Project Sponsor | TBD | Authorise merge into spec master |
| Commercial Review | TBD | Approve §37 Business Subscription tier prices |
| Legal Review | TBD | Review §33.3 amended consent text against current D-05 and D-06 |
