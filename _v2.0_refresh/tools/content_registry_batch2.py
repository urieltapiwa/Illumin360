"""Batch 2 — NFR, Architecture, Integration, DB, API, Security, AI Services, Sales."""

BATCH2 = {}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-02-003 — Non-Functional Requirements
# ════════════════════════════════════════════════════════════════════════════
BATCH2["02_Requirements/Non_Functional_Requirements/ILLM-02-003_Non_Functional_Requirements_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §17 (NFRs) plus standards alignment §35.",
    "sections": {
        "2. Purpose": {
            "narrative": "This document specifies non-functional requirements covering performance, security, scalability, availability, maintainability, usability, and compliance. Each NFR is testable. Functional requirements are in ILLM-02-002.",
            "rows": [
                ["Purpose", "Specify testable NFRs for the Illumin360 platform", "Architect", "Active"],
                ["Standards orientation", "ISO/IEC 27001, ISO 9241, OpenAPI 3.x, microservices/DDD/Clean/REST", "Architect", "Required"],
            ],
        },
        "3. Scope": {
            "narrative": "Applies to all five portals and all back-end services. Targets are measured at the API gateway boundary (latency) and via observability instrumentation (errors, throughput).",
            "rows": [
                ["Scope", "All portals + back-end services", "—", "Defined"],
                ["Measurement boundary", "API gateway for latency; OpenTelemetry traces for service-internal", "—", "Defined"],
            ],
        },
        "4. Non-Functional Requirements": {
            "narrative": "NFR families grouped by quality attribute. Each family has detailed rows below.",
            "rows": [
                ["NFR-P Performance", "P95 latencies, matching throughput, report-gen time", "Architect", "Defined"],
                ["NFR-S Security", "Keycloak-based auth, encryption, PCI-DSS SAQ A, audit", "Architect", "Defined"],
                ["NFR-C Scalability", "RabbitMQ async, MinIO storage, K8s horizontal scaling", "Architect", "Defined"],
                ["NFR-A Availability", "99.5% uptime target initial; 99.9% by Year 2", "Architect", "Defined"],
                ["NFR-M Maintainability", "Clean Architecture, DDD, OpenAPI contracts, .NET conventions", "Architect", "Defined"],
                ["NFR-U Usability", "ISO 9241; WCAG 2.1 AA; PWA install support", "Architect", "Defined"],
                ["NFR-CO Compliance", "Labour Act, ETA, PCI-DSS SAQ A, Section 31 branding policy", "Legal", "Defined"],
                ["NFR-D Data", "Audit immutability 7y; backups daily 30d; CV retention per §12 D-12", "Architect", "Defined"],
            ],
        },
        "5. Performance Requirements": {
            "narrative": "Performance targets per v3.7 spec §17.2 with measurement methodology.",
            "rows": [
                ["NFR-P-1 Matching engine", "P95 ≤ 30 seconds for pool of 5,000 candidates", "QA via k6", "Target"],
                ["NFR-P-2 API latency (non-AI)", "P95 < 300ms at gateway boundary", "QA via k6 + Tempo", "Target"],
                ["NFR-P-3 Report generation", "PDF + Word complete within 60 seconds", "QA", "Target"],
                ["NFR-P-4 File upload", "Direct-to-MinIO via presigned URL; server bandwidth not a bottleneck", "Architect", "Designed"],
                ["NFR-P-5 Page load", "P95 first contentful paint ≤ 2 seconds on 3G connection (PWA)", "QA via Playwright", "Target"],
                ["NFR-P-6 Database query", "P99 < 100ms for primary lookup queries; P95 < 50ms for index scans", "DBA", "Target"],
                ["NFR-P-7 Cron jobs", "Daily 06:00 WAT job completes within 5 minutes for 50k subscriptions", "Architect", "Target"],
                ["NFR-P-8 AI Assistant", "First-token streaming begins within 1s of message submit", "QA", "Target"],
            ],
        },
        "6. Security Requirements": {
            "narrative": "Security NFRs anchored on Keycloak IAM and ISO/IEC 27001 control families. Detailed design in ILLM-03-007.",
            "rows": [
                ["NFR-S-1 Authentication", "All access through Keycloak — OIDC tokens; no platform-stored passwords", "Architect", "Mandatory"],
                ["NFR-S-2 Authorisation", "RBAC enforced at gateway and re-checked per service", "Architect", "Mandatory"],
                ["NFR-S-3 MFA", "Mandatory for admin and support realms; optional for others", "Architect", "Mandatory"],
                ["NFR-S-4 Token TTL", "Access 15min; refresh 30d (Keycloak-managed)", "Architect", "Mandatory"],
                ["NFR-S-5 Encryption in transit", "TLS 1.2+ minimum; HSTS headers; 1.3 preferred", "Architect", "Mandatory"],
                ["NFR-S-6 Encryption at rest", "AES-256 for id_number, student_number (pgcrypto)", "Architect", "Mandatory"],
                ["NFR-S-7 Virus scanning", "ClamAV on every uploaded file before further processing", "Architect", "Mandatory"],
                ["NFR-S-8 Webhook verification", "HMAC verified on every payment webhook before processing", "Architect", "Mandatory"],
                ["NFR-S-9 Audit immutability", "audit_logs and compliance_justifications append-only; no UI delete path", "Architect", "Mandatory"],
                ["NFR-S-10 Secret management", "All secrets in HashiCorp Vault; never in source code or env files", "Architect", "Mandatory"],
                ["NFR-S-11 Rate limiting", "100/min/user; 10/min auth; 5/min/IP for public portal", "Architect", "Mandatory"],
                ["NFR-S-12 Container security", "Trivy scan in CI; non-root runtime; read-only root filesystem where possible", "DevOps", "Mandatory"],
            ],
        },
        "7. Scalability Requirements": {
            "narrative": "Scalability targets for capacity planning across the 8-phase plan.",
            "rows": [
                ["NFR-C-1 Concurrent users", "Year 1: 500 concurrent; Year 3: 5,000 concurrent", "Architect", "Target"],
                ["NFR-C-2 Candidate pool size", "Year 1: 5k; Year 3: 50k; Year 5: 200k", "Architect", "Target"],
                ["NFR-C-3 Requests per day", "Year 1: 100/day; Year 3: 1,000/day", "Architect", "Target"],
                ["NFR-C-4 Horizontal scaling", "Stateless services scale via K8s HPA on CPU/memory", "DevOps", "Designed"],
                ["NFR-C-5 Database scaling", "Partition large tables (audit_logs, auto_application_matches) by month", "DBA", "Designed"],
                ["NFR-C-6 Object storage scaling", "MinIO erasure coding; tiered storage for older CVs", "DevOps", "Designed"],
                ["NFR-C-7 Queue throughput", "RabbitMQ cluster sized for 10k messages/min sustained", "DevOps", "Designed"],
                ["NFR-C-8 Job idempotency", "All async consumers idempotent; safe to retry on failure", "Architect", "Mandatory"],
            ],
        },
        "8. Assumptions and Constraints": {
            "narrative": "Underlying assumptions for the NFR set plus binding constraints.",
            "rows": [
                ["Assumption — hosting", "Cloud or co-located infrastructure with sufficient capacity", "DevOps", "Assumed"],
                ["Assumption — connectivity", "Sufficient bandwidth across Namibia for PWA experience", "Architect", "Assumed"],
                ["Constraint — PCI-DSS SAQ A", "No card data on platform — hosted gateway model", "Architect", "Mandatory"],
                ["Constraint — Namibian jurisdiction", "Hosting region and data residency comply with local law", "Legal", "Required"],
                ["Constraint — Section 31 branding", "Client-facing artefacts free of AI/vendor references", "Marketing", "Mandatory"],
                ["Constraint — open-source preference", "Runtime stack OSS where feasible per §36", "Architect", "Preferred"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-03-001 — Architecture Diagrams
# ════════════════════════════════════════════════════════════════════════════
BATCH2["03_System_Design/High_Level_Design/Architecture_Diagrams/ILLM-03-001_Architecture_Diagrams_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §35 bounded-context map. Microservices architecture aligned to DDD with Clean Architecture layering per service.",
    "sections": {
        "2. Architecture Overview": {
            "narrative": "The Illumin360 platform is a microservices architecture aligned to Domain-Driven Design bounded contexts. Each service owns a context, owns its data, and exposes APIs through an API gateway. Async communication flows via RabbitMQ; sync via REST. Clean Architecture layering applies within each service — domain layer is independent of frameworks and infrastructure. The five portals (Professional, Student, Business, Administrator, Support) consume the same back-end services through scoped Keycloak-validated tokens.",
            "rows": [
                ["Architecture style", "Microservices aligned to DDD bounded contexts", "Architect", "Confirmed"],
                ["Inter-service comms", "Async events via RabbitMQ; sync queries via REST through API gateway", "Architect", "Confirmed"],
                ["Per-service architecture", "Clean Architecture — Domain / Application / Infrastructure layers", "Architect", "Confirmed"],
                ["External-traffic edge", "Kong or Traefik API gateway with Keycloak token validation", "Architect", "Confirmed"],
                ["Frontend", "Five portals consuming the same back-end APIs with scoped tokens", "Architect", "Confirmed"],
                ["Standards", "DDD, Clean, Microservices, REST, OpenAPI 3.x", "Architect", "Mandatory"],
            ],
        },
        "3. Architecture Diagrams": {
            "narrative": "Diagrams to be produced as separate visual artefacts and embedded in this section. The table below catalogues the diagram inventory. Each diagram is maintained in a separate diagram source file (Mermaid or PlantUML) and exported as PNG for inclusion.",
            "rows": [
                ["DIAG-1 System context", "Five portals, external systems (Keycloak, payment gateway, AI services), platform boundary", "Architect", "To produce"],
                ["DIAG-2 Bounded context map", "13 bounded contexts with relationships (customer/supplier, conformist, anti-corruption layer)", "Architect", "To produce"],
                ["DIAG-3 Microservices deployment", "K8s pod topology, service-to-service communication paths", "Architect", "To produce"],
                ["DIAG-4 Data flow — registration", "Professional registration end-to-end including Founder grant", "Architect", "To produce"],
                ["DIAG-5 Data flow — Auto-Application", "Request creation through matching to audit ledger", "Architect", "To produce"],
                ["DIAG-6 Data flow — internal portal close", "Cron close → matching → report generation → notification", "Architect", "To produce"],
                ["DIAG-7 Data flow — payment", "Webhook-driven payment confirmation flow", "Architect", "To produce"],
                ["DIAG-8 Component — matching engine", "Two-pass pipeline with custom weights and gap analysis branches", "Architect", "To produce"],
                ["DIAG-9 Sequence — AI Assistant", "User message through context assembly, inference, post-gen filter, response", "Architect", "To produce"],
                ["DIAG-10 Observability stack", "OpenTelemetry → Prometheus / Loki / Tempo → Grafana", "Architect", "To produce"],
            ],
        },
        "4. Architecture Decisions": {
            "narrative": "Key architecture decisions captured here at headline level. Full ADRs maintained in the Decision Log (ILLM-13-008).",
            "rows": [
                ["AD-1 Microservices aligned to DDD", "13 services per spec §35; deployable independently", "Architect", "Decided"],
                ["AD-2 Keycloak as IAM authority", "OIDC for all portals; platform doesn't store passwords", "Architect", "Decided"],
                ["AD-3 RabbitMQ for async", "Durable, routing-flexible message queue", "Architect", "Decided"],
                ["AD-4 PostgreSQL + pgvector", "Relational + vector search in one database", "Architect", "Decided"],
                ["AD-5 MinIO for object storage", "S3-compatible, on-prem option, free egress", "Architect", "Decided"],
                ["AD-6 Grafana LGTM stack", "Unified observability vendor", "Architect", "Decided"],
                ["AD-7 Temporal for long workflows", "Founder grant, portal close, subscription lifecycle", "Architect", "Decided"],
                ["AD-8 API gateway pattern", "Kong or Traefik with OIDC validation and rate limiting", "Architect", "Open — pick one"],
                ["AD-9 Blind-screening structural", "Photo not in matching projection — compliance by design", "Architect", "Decided"],
                ["AD-10 Founder quota serialisation", "SELECT FOR UPDATE; race-condition tested", "Architect", "Decided"],
                ["AD-11 .NET application language", "Implied by §35 standards list — pending explicit confirmation", "Architect", "Pending"],
            ],
        },
        "5. Quality Attributes": {
            "narrative": "Quality attribute targets that drive architecture choices.",
            "rows": [
                ["Availability", "99.5% Year 1; 99.9% by Year 2", "DevOps", "Target"],
                ["Performance", "Matching P95 30s/5k; API P95 <300ms", "Architect", "Target"],
                ["Scalability", "Horizontal scaling on stateless services; partition large tables", "Architect", "Designed"],
                ["Security", "Keycloak + Vault + ClamAV + audit; ISO 27001 control mapping", "Architect", "Designed"],
                ["Maintainability", "Clean Architecture per service; OpenAPI contracts; .NET conventions", "Architect", "Designed"],
                ["Observability", "OpenTelemetry instrumentation; LGTM stack visibility", "Architect", "Designed"],
                ["Auditability", "Append-only audit_logs; 7-year retention on compliance entries", "Architect", "Designed"],
                ["Compliance", "PCI-DSS SAQ A; Labour Act 11/2007; ETA 4/2019; ISO 27001 alignment", "Legal", "Mandatory"],
            ],
        },
        "6. Component Descriptions": {
            "narrative": "Each microservice and its bounded context. See spec §35 for the canonical list.",
            "rows": [
                ["identity-svc", "Keycloak adapter; profile shell; role mapping", "Architect", "Service"],
                ["professional-svc", "Professional + Student profile, CV, skills, badges", "Architect", "Service"],
                ["business-svc", "Business profile, internal portal config, logo", "Architect", "Service"],
                ["recruitment-svc", "Recruitment requests, compliance justifications", "Architect", "Service"],
                ["matching-svc", "Matching engine, Auto-Application, adaptive weighting, gap analysis", "Architect", "Service"],
                ["reporting-svc", "PDF + Word report generation; MinIO output", "Architect", "Service"],
                ["payment-svc", "Payment initiation, webhook, invoicing, subscriptions", "Architect", "Service"],
                ["notification-svc", "Email + in-product notifications via RabbitMQ", "Architect", "Service"],
                ["ai-assistant-svc", "Conversational assistant; session; escalation", "Architect", "Service"],
                ["engagement-svc", "Badges, social features, referrals, demand feed, spotlight", "Architect", "Service"],
                ["benchmarking-svc", "Workforce snapshots; Professional and Student rankings", "Architect", "Service"],
                ["support-svc", "Tickets, KB, audit, identity-verification workflow", "Architect", "Service"],
                ["admin-svc", "Admin dashboard, reports, read-mostly audit views", "Architect", "Service"],
            ],
        },
        "7. Review and Approval": {
            "narrative": "Architecture document review and approval per the design review process.",
            "rows": [
                ["Architect", "Bounded context map and service decomposition reviewed", "Software Engineer & Architect", "Drafted"],
                ["DevOps", "K8s deployment topology feasible", "DevOps", "Pending"],
                ["Sponsor", "Architecture aligns with business case and budget", "Sponsor", "Pending"],
                ["Diagrams sign-off", "Required after DIAG-1 through DIAG-10 are produced", "Architect", "Pending"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-03-003 — Integration Architecture
# ════════════════════════════════════════════════════════════════════════════
BATCH2["03_System_Design/High_Level_Design/Integration_Architecture/ILLM-03-003_Integration_Architecture_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §28 (third-party AI services) and §11 (payment gateway). This is an internal technical document — explicit vendor names are permitted here per §31 branding policy (developer integration documentation is exempt).",
    "sections": {
        "2. Integration Overview": {
            "narrative": "Illumin360 integrates with three external systems at runtime: the Anthropic Claude Sonnet 4.6 API for CV analysis, justification narratives, and the Platform Assistant; the Google Cloud Vision API for OCR on scanned CVs; and a Namibian payment gateway (selection pending) for hosted-page card payments. Keycloak is also external but is operated as part of the platform infrastructure and not treated as a third-party integration here. All external calls are made through provider-specific adapters in the relevant microservices, with retry/circuit-breaker (Polly) protection and OpenTelemetry instrumentation.",
            "rows": [
                ["Anthropic Claude Sonnet 4.6", "CV analysis + justification engine + AI Platform Assistant", "professional-svc, matching-svc, ai-assistant-svc", "Confirmed"],
                ["Google Cloud Vision", "OCR for scanned CVs (DOCUMENT_TEXT_DETECTION)", "professional-svc", "Confirmed"],
                ["Payment gateway (TBD)", "Hosted card-payment page; webhook-driven confirmation", "payment-svc", "Open"],
                ["Keycloak", "OIDC token issuer (platform infrastructure)", "All services", "Confirmed"],
                ["Email provider", "Transactional email delivery", "notification-svc", "Open"],
            ],
        },
        "3. Claude Sonnet 4.6 Integration": {
            "narrative": "Anthropic Claude Sonnet 4.6 powers three distinct workloads. Each has its own system prompt and access pattern. The model string is pinned to 'claude-sonnet-4-6' — no aliases. Per spec §31 branding policy, this vendor reference is internal-only and never appears in client-facing content; the AI Assistant in particular is instructed never to identify itself as Claude.",
            "rows": [
                ["Workload — CV analysis", "Async via RabbitMQ queue; structured JSON extraction; batch API discount", "professional-svc", "Phase 1"],
                ["Workload — Justification narrative", "Per shortlisted candidate; 3–6 sentence narrative grounded in structured data", "matching-svc", "Phase 2"],
                ["Workload — Platform Assistant", "Streaming inference; prompt caching for system prompt; per-instance config", "ai-assistant-svc", "Phase 6"],
                ["Authentication", "API key stored in HashiCorp Vault; environment-injected", "DevOps", "Required"],
                ["Retry policy", "30s timeout; one retry; admin notification on second failure", "Architect", "Designed"],
                ["Fallback", "Keyword matching for CV analysis; template-based justification; assistant unavailable banner", "Architect", "Designed"],
                ["Cost monitoring", "Year 1 ≈ USD 4.25/month combined; alert at 200% YoY", "Finance", "Monitor"],
                ["Privacy", "CV text and prompt context sent to Anthropic — disclosed in D-05 consent", "Legal", "Documented"],
                ["Identity policy compliance", "Assistant system prompt prohibits self-identification as Claude; post-gen filter scans response", "Architect", "Mandatory"],
            ],
        },
        "4. Google Cloud Vision Integration": {
            "narrative": "Google Cloud Vision provides OCR fallback when standard text extraction (pdfplumber for PDF, python-docx for Word) yields fewer than 50 words. Triggered automatically; never the primary text-extraction path. Tesseract OCR is the offline fallback if Cloud Vision is unavailable.",
            "rows": [
                ["Feature used", "DOCUMENT_TEXT_DETECTION — optimised for full-page documents", "professional-svc", "Confirmed"],
                ["Trigger condition", "Standard extraction returns < 50 words OR non-alphanumeric ratio high", "Architect", "Designed"],
                ["Authentication", "GCP service account JSON in Vault", "DevOps", "Required"],
                ["Timeout", "60 seconds for multi-page PDFs", "Architect", "Configured"],
                ["Page cap", "First 5 pages only — admin review flag for CVs exceeding 5 pages", "Architect", "Designed"],
                ["Confidence threshold", "< 80% triggers candidate notification to review CV upload quality", "Architect", "Designed"],
                ["Fallback", "Tesseract OCR (local) if Cloud Vision is unavailable; admin alert", "Architect", "Designed"],
                ["Cost", "Free tier covers first 1,000 pages/month; USD 1.50/1,000 thereafter", "Finance", "Monitor"],
                ["Privacy", "Document pages sent to Google for processing — disclosed in D-05 consent", "Legal", "Documented"],
            ],
        },
        "5. Payment Gateway Integration": {
            "narrative": "Hosted-page payment integration following PCI-DSS SAQ A. No card data ever touches the platform. The gateway redirect URL is for UX only; signed webhook is the authoritative confirmation. Specific gateway selection pending.",
            "rows": [
                ["Model", "Hosted payment page; webhook-driven confirmation", "Architect", "Designed"],
                ["Selection criteria", "Namibian gateway; hosted page; HMAC-signed webhooks; 3DS support", "Architect", "Open"],
                ["Authentication", "API key + webhook secret in Vault", "DevOps", "Required"],
                ["Flow", "Initiate → redirect → user pays → gateway fires signed webhook → platform verifies HMAC → activate subscription / unlock report / reveal candidate", "Architect", "Designed"],
                ["Idempotency", "Webhook handler idempotent on (gateway_reference, status) tuple", "Architect", "Mandatory"],
                ["Refund handling", "Manual admin-initiated refunds via gateway API", "payment-svc", "Phase 3"],
                ["PCI-DSS scope", "SAQ A only — no card data on platform", "Compliance", "Mandatory"],
            ],
        },
        "6. Integration Testing Requirements": {
            "narrative": "Each integration has contract tests against a sandbox or recorded fixtures. Production smoke tests verify each integration on every deploy.",
            "rows": [
                ["Anthropic — contract test", "Recorded fixture for CV-extraction JSON schema; smoke test against live API", "QA", "Required"],
                ["Anthropic — assistant identity", "Test asks 'are you Claude?' — asserts fallback wording per ILLM-03-013 §7", "QA", "Required"],
                ["Google Vision — contract test", "Recorded fixture; smoke test against live API", "QA", "Required"],
                ["Payment gateway — sandbox", "Sandbox account exercising success, failure, refund, 3DS challenge", "QA", "Required"],
                ["Webhook HMAC — replay/tampering", "Replay attack and tampered payload tests both rejected", "QA", "Required"],
                ["Keycloak — token revocation", "Revoked refresh token cannot mint new access token", "QA", "Required"],
            ],
        },
        "7. Fallback Strategy": {
            "narrative": "Each integration has a documented fallback. Failures are observable via Grafana and the on-call rotation responds per the incident runbook.",
            "rows": [
                ["Anthropic CV analysis unavailable", "Fall back to keyword matching; log fallback event; queue for retry when service restores", "Architect", "Designed"],
                ["Anthropic justification unavailable", "Fall back to template-assembled justification; mark report as 'standard methodology'", "Architect", "Designed"],
                ["Platform Assistant unavailable", "Display banner; user can still escalate to support; conversation suspended", "Architect", "Designed"],
                ["Google Vision unavailable", "Use local Tesseract OCR; flag if confidence < 80%; admin alert", "Architect", "Designed"],
                ["Payment gateway unavailable", "Block new payment attempts with user-friendly message; admin alert; resume when service restores", "Architect", "Designed"],
                ["Keycloak unavailable", "All authenticated traffic blocked at gateway; emergency token-replay tolerance not used", "Architect", "Designed"],
                ["Optional Phase 8 — Ollama self-hosted fallback", "Local model for AI Assistant degraded mode during vendor outages", "Architect", "Optional"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-03-004 — Database Design
# ════════════════════════════════════════════════════════════════════════════
BATCH2["03_System_Design/Detailed_Design/Database_Design/ILLM-03-004_Database_Design_v1_0.docx"] = {
    "v2_change_description": "Populated from illumin360_master_migrations_v3.6.sql (70 migrations) plus v3.7 additions for Auto-Application, Business subscriptions, Support, and Benchmarking domains.",
    "sections": {
        "2. Database Schema Overview": {
            "narrative": "The platform uses PostgreSQL 15+ as the primary relational store with pgvector for semantic similarity. The schema is organised into seven domains aligned to the DDD bounded contexts. The canonical migration source is illumin360_master_migrations_v3.6.sql — currently 70 migrations covering Identity & Access, Candidate Profile, Recruitment, Finance, System Logs, AI & Engagement, plus v3.7 additions for Auto-Application, Business Subscriptions, Support, and Benchmarking. All schema changes are additive and idempotent where possible.",
            "rows": [
                ["Engine", "PostgreSQL 15+", "DBA", "Confirmed"],
                ["Extensions", "uuid-ossp, pgcrypto, pgvector", "DBA", "Confirmed"],
                ["Migration tool", "Sequential numbered .sql files; idempotent IF NOT EXISTS guards", "DBA", "Confirmed"],
                ["Canonical source", "illumin360_master_migrations_v3.6.sql in Draft Concept folder", "Architect", "Active"],
                ["Encryption at rest", "AES-256 via pgcrypto for id_number, student_number; passwords owned by Keycloak", "Architect", "Confirmed"],
                ["Partitioning strategy", "Monthly partitions on audit_logs, auto_application_matches at scale", "DBA", "Phase 3"],
            ],
        },
        "3. Table Definitions": {
            "narrative": "Tables grouped by domain. Detailed DDL is in the master migrations file. Top-level inventory below.",
            "rows": [
                ["users", "Identity profile shell — Keycloak sub is the foreign reference; v1.0", "identity-svc", "Confirmed"],
                ["job_seekers", "Professional/Student profile; profile_type discriminator; v1.0 + v3.3 social fields", "professional-svc", "Confirmed"],
                ["employers", "Business profile; logo metadata; v1.0 + v3.4 logo timestamps", "business-svc", "Confirmed"],
                ["candidate_profiles", "Profile detail; qualification, experience; v1.0 + v3.0 student-specific fields", "professional-svc", "Confirmed"],
                ["candidate_city_preferences, candidate_skills, candidate_languages, candidate_qualifications", "Profile facets; v1.0", "professional-svc", "Confirmed"],
                ["candidate_documents", "CV uploads; processing_status; v1.0 + v3.5 AI processing fields", "professional-svc", "Confirmed"],
                ["subscriptions, subscription_reminders", "Professional/Student subscription state and reminder deduplication; v1.0", "payment-svc", "Confirmed"],
                ["pricing_plans", "All pricing tiers including v3.6 founder_permanent", "payment-svc", "Confirmed"],
                ["payments, invoices, receipts", "Financial transactions; v1.0", "payment-svc", "Confirmed"],
                ["recruitment_requests, recruitment_request_filters, compliance_justifications", "Request lifecycle; v1.0 + v3.4 custom_weights, weights_locked", "recruitment-svc", "Confirmed"],
                ["uploaded_request_cvs", "Business-uploaded CVs for search modes 2 & 3; v1.0", "recruitment-svc", "Confirmed"],
                ["internal_applications", "Internal portal applications; v2.0", "recruitment-svc", "Confirmed"],
                ["candidate_matches", "Match scores per (request, candidate); v1.0 + v3.4 weights_used, gap_analysis", "matching-svc", "Confirmed"],
                ["shortlists, reports", "Generated shortlists and report metadata; v1.0", "reporting-svc", "Confirmed"],
                ["email_logs, notification_logs", "Email and in-product notification history; v1.0", "notification-svc", "Confirmed"],
                ["audit_logs", "Immutable append-only audit trail; v1.0", "all services write", "Confirmed"],
                ["student_verifications, institution_email_domains", "Student lifecycle; v3.0", "professional-svc, support-svc", "Confirmed"],
                ["candidate_badges, employer_badges", "Gamification badges; v3.3 + v3.4 + v3.6 founder badges", "engagement-svc", "Confirmed"],
                ["referrals", "Referral programme; v3.3", "engagement-svc", "Confirmed"],
                ["insights, spotlight_features, demand_feed_cache", "Social features F2/F3/F5; v3.3", "engagement-svc", "Confirmed"],
                ["match_feedback", "RLHF data collection; v3.4", "matching-svc", "Confirmed"],
                ["candidate_videos", "Phase 7 video pitch + transcription; v3.4", "professional-svc", "Confirmed"],
                ["assistant_conversations, assistant_prompts, ai_processing_log", "AI Platform Assistant; v3.5 + v3.6", "ai-assistant-svc", "Confirmed"],
                ["platform_config", "Admin-configurable platform settings; v3.6", "admin-svc", "Confirmed"],
                ["founder_registrations", "Founder Programme quota and grants; v3.6", "professional-svc/business-svc", "Confirmed"],
                ["auto_application_matches", "Auto-Application audit ledger; v3.7 NEW", "matching-svc", "v3.7"],
                ["candidate_auto_preferences", "Auto-Application opt-outs and notification tier; v3.7 NEW", "professional-svc", "v3.7"],
                ["business_subscriptions, business_subscription_history", "Business subscription tier model; v3.7 NEW", "payment-svc", "v3.7"],
                ["support_staff, support_tickets, support_messages, support_attachments, knowledge_articles", "Support Portal domain; v3.7 NEW", "support-svc", "v3.7"],
                ["benchmark_snapshots, professional_rank_history, student_leaderboard_entries", "Benchmarking domain; v3.7 NEW", "benchmarking-svc", "v3.7"],
            ],
        },
        "4. Data Dictionary": {
            "narrative": "Selected key columns with semantics — full data dictionary maintained as inline column comments in migrations and exposed via OpenAPI schemas. Highlights below.",
            "rows": [
                ["users.id", "UUID — primary key; also foreign reference target for all user-scoped tables", "—", "v1.0"],
                ["job_seekers.profile_type", "ENUM job_seeker|student — discriminator for Professional vs Student profile", "—", "v1.0"],
                ["job_seekers.is_founder", "BOOLEAN — bypasses subscription expiry checks if true", "—", "v3.6"],
                ["recruitment_requests.custom_weights", "JSONB nullable — adaptive weights; NULL = standard schedule", "—", "v3.4"],
                ["recruitment_requests.weights_locked", "BOOLEAN — set on submission; weights immutable after", "—", "v3.4"],
                ["candidate_matches.weights_used", "JSONB NOT NULL — immutable audit of weights applied to this match", "—", "v3.4"],
                ["candidate_matches.gap_analysis", "JSONB nullable — populated only for 70–85% band candidates", "—", "v3.4"],
                ["founder_registrations.founder_number", "INTEGER — sequential per user_type; 1..300 Professional, 1..50 Business", "—", "v3.6"],
                ["audit_logs.event_type", "VARCHAR — event taxonomy (sensitive_filter_used, candidate_unlock, report_generated, etc.)", "—", "v1.0"],
                ["compliance_justifications.justification_text", "TEXT — minimum 50 words; immutable", "—", "v1.0"],
                ["auto_application_matches.inclusion_reason", "VARCHAR — why candidate was considered (hard_filter_pass, opt_in, etc.)", "—", "v3.7"],
                ["auto_application_matches.exclusion_reason", "VARCHAR — why not shortlisted (industry_optout, employer_optout, score_below_threshold, etc.)", "—", "v3.7"],
            ],
        },
        "5. Relationships and Constraints": {
            "narrative": "Key referential integrity and constraint patterns. Full ER diagram in ILLM-03-001 DIAG-2.",
            "rows": [
                ["users → job_seekers/employers/support_staff", "1:0..1 — exactly one profile per user", "Architect", "Enforced"],
                ["job_seekers → candidate_profiles", "1:1 — UNIQUE", "Architect", "Enforced"],
                ["recruitment_requests → employers", "Many:1 — FK", "Architect", "Enforced"],
                ["candidate_matches → recruitment_requests + job_seekers", "Many:1 each", "Architect", "Enforced"],
                ["match_feedback (employer_id, match_id)", "UNIQUE — one feedback per pair", "Architect", "Enforced"],
                ["candidate_badges (job_seeker_id, badge_type) WHERE revoked_at IS NULL", "Partial UNIQUE — one active per type", "Architect", "Enforced"],
                ["employer_badges (employer_id, badge_type) WHERE revoked_at IS NULL", "Partial UNIQUE", "Architect", "Enforced"],
                ["founder_registrations (user_type, founder_number)", "UNIQUE", "Architect", "Enforced"],
                ["audit_logs", "No FK enforcement (append-only forensic record); ON DELETE forbidden", "Architect", "Mandatory"],
                ["compliance_justifications", "No UPDATE or DELETE allowed by application; row-level security", "Architect", "Mandatory"],
            ],
        },
        "6. Indexing Strategy": {
            "narrative": "Indexes follow the matching engine's hard-filter query plan plus admin dashboard read patterns.",
            "rows": [
                ["users(email)", "Unique B-tree — login lookup", "DBA", "Confirmed"],
                ["job_seekers(city)", "B-tree — hard filter Pass 1", "DBA", "Confirmed"],
                ["job_seekers(availability_status)", "B-tree — hard filter", "DBA", "Confirmed"],
                ["candidate_profiles(nqf_level)", "B-tree — hard filter on qualification minimum", "DBA", "Confirmed"],
                ["candidate_skills(job_seeker_id, skill_name)", "Composite B-tree — skill lookup", "DBA", "Confirmed"],
                ["audit_logs(event_type, created_at DESC)", "Composite — admin filtering", "DBA", "Confirmed"],
                ["assistant_conversations(user_id, started_at DESC)", "Composite — user history lookup", "DBA", "Confirmed"],
                ["match_feedback(scoring_model, created_at)", "Composite — Phase 8 coverage analytics", "DBA", "Confirmed"],
                ["auto_application_matches(request_id)", "B-tree — per-request ledger reads", "DBA", "v3.7"],
                ["pgvector indexes on embedding columns (Phase 6+)", "IVFFlat or HNSW — semantic similarity", "DBA", "Phase 6"],
            ],
        },
        "7. Migration Strategy": {
            "narrative": "Sequential numbered migrations applied in order. Each migration includes an idempotent guard (IF NOT EXISTS or equivalent). Rollback migrations maintained alongside (ILLM-07-012).",
            "rows": [
                ["Numbering", "Migrations 001–070 cover v1.0 through v3.6; 071+ cover v3.7 additions", "DBA", "Convention"],
                ["Application order", "Strict numeric order; cannot skip", "DBA", "Mandatory"],
                ["Idempotency", "IF NOT EXISTS guards; safe to re-run on a partially-migrated database", "DBA", "Mandatory"],
                ["Rollback", "Each forward migration has a paired rollback file in ILLM-07-012", "DBA", "Mandatory"],
                ["Production deploy", "Applied as part of CI/CD pipeline; service deploy waits for migration success", "DevOps", "Designed"],
                ["Backfills", "Non-trivial backfills (e.g., candidate_matches.weights_used) split into separate migrations", "DBA", "Pattern"],
                ["v3.7 migrations (planned 071-085)", "auto_application_matches, candidate_auto_preferences, business_subscriptions, business_subscription_history, support_staff, support_tickets, support_messages, support_attachments, knowledge_articles, benchmark_snapshots, professional_rank_history, student_leaderboard_entries, skill_quest_assignments, employer_reviews", "DBA", "Pending"],
            ],
        },
        "8. Review and Approval": {
            "narrative": "DB design sign-off gate.",
            "rows": [
                ["Architect", "Schema aligns with bounded contexts and FRs", "Architect", "Drafted"],
                ["DBA", "Performance and indexing strategy feasible", "DBA", "Pending"],
                ["Security", "Encryption and audit immutability verified", "Security", "Pending"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-03-005 — API Design
# ════════════════════════════════════════════════════════════════════════════
BATCH2["03_System_Design/Detailed_Design/API_Design/ILLM-03-005_API_Design_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §14 (API endpoint design) plus v3.7 additions for Support Portal, Auto-Application opt-outs, Business subscriptions, Benchmarking.",
    "sections": {
        "2. API Standards": {
            "narrative": "All public APIs follow REST conventions per spec §35 and are specified in OpenAPI 3.x. The OpenAPI spec is the contract — server stubs and client SDKs are generated from it. Versioning is in the URI path. Authentication via Keycloak-issued OIDC tokens validated at the API gateway.",
            "rows": [
                ["Style", "REST — resource URIs, standard verbs, HTTP status codes", "Architect", "Mandatory"],
                ["Specification", "OpenAPI 3.x — single source of truth", "Architect", "Mandatory"],
                ["Versioning", "URI path — /api/v1, /api/v2", "Architect", "Confirmed"],
                ["Auth", "OIDC Bearer tokens issued by Keycloak; validated at gateway", "Architect", "Confirmed"],
                ["Content type", "application/json; multipart/form-data for file upload only", "Architect", "Confirmed"],
                ["Timestamps", "ISO 8601 UTC", "Architect", "Confirmed"],
                ["Pagination", "Cursor-based for large collections; offset/limit for small", "Architect", "Standard"],
                ["Naming", "snake_case in JSON bodies; aligns with database identifiers", "Architect", "Standard"],
            ],
        },
        "3. API Endpoint Catalogue": {
            "narrative": "Catalogued by domain. Full OpenAPI spec generated from server-side definitions and published via ILLM-12-002 API Documentation.",
            "rows": [
                ["Auth (8 endpoints)", "register, register/student, login, refresh, logout, forgot-password, reset-password, verify-email — most proxied to Keycloak", "identity-svc", "Phase 1"],
                ["Professional (Job Seeker, 15 endpoints)", "Profile, subscription, CV management, city/skill prefs, dashboard, payment history, photo, video", "professional-svc", "Phase 1–7"],
                ["Auto-Application preferences (3 endpoints)", "GET/PUT preferences; pause/resume", "professional-svc", "Phase 1"],
                ["Student (5 endpoints)", "Verification status, graduation upgrade flow, leaderboard view", "professional-svc", "Phase 5"],
                ["Business (Employer, 21 endpoints)", "Profile, dashboard, request lifecycle, shortlist/report access, CV uploads, internal portal, invoice/receipt", "business-svc + recruitment-svc", "Phase 1–4"],
                ["Business subscription (5 endpoints)", "Tier selection, current allowance, history, upgrade, downgrade", "payment-svc", "Phase 2"],
                ["Benchmarking (4 endpoints)", "Workforce snapshot upload, report retrieval, Professional rank, Student leaderboard", "benchmarking-svc", "Phase 5–6"],
                ["Public portal (2 endpoints)", "GET /apply/:token — vacancy details; POST /apply/:token/submit", "recruitment-svc", "Phase 4"],
                ["Admin (20 endpoints)", "User management, subscriptions, reports, payments/refunds, compliance, audit, verification, analytics, pricing, templates", "admin-svc", "Phase 3"],
                ["Support (12 endpoints — NEW)", "Ticket CRUD, message thread, KB authoring, identity verification workflow", "support-svc", "Phase 5"],
                ["AI Assistant (3 endpoints)", "POST /assistant/message (SSE stream), GET /assistant/session/:id, POST /assistant/escalate", "ai-assistant-svc", "Phase 6"],
                ["Payment & webhook (3 endpoints)", "POST /payments/initiate, GET /payments/:id/status, POST /webhooks/payment (HMAC verified)", "payment-svc", "Phase 2"],
                ["Public — Founder status (1 endpoint)", "GET /public/founder/status — homepage quota widget", "professional-svc/business-svc", "Phase 1"],
                ["Public — Profile card (2 endpoints)", "GET /public/p/:username (HTML), GET /public/p/:username/card.png", "engagement-svc", "Phase 5"],
                ["Public — Demand feed (1 endpoint)", "GET /public/demand-feed — weekly anonymised signals", "engagement-svc", "Phase 5"],
                ["Public — Insights blog (2 endpoints)", "GET /public/insights, GET /public/insights/:slug", "engagement-svc", "Phase 5"],
                ["Feedback (3 endpoints)", "POST /employers/me/requests/:id/feedback; admin views", "matching-svc, admin-svc", "Phase 6"],
            ],
        },
        "4. Request and Response Schemas": {
            "narrative": "All request and response schemas defined in the OpenAPI spec. Key schemas summarised below.",
            "rows": [
                ["RecruitmentRequestSubmit", "filters[], custom_weights{}, declaration_confirmed (D-03)", "recruitment-svc", "Phase 1"],
                ["ShortlistResponse — preview", "candidates[].{anonymous_id, match_score, sub_scores}; no PII", "matching-svc", "Phase 1"],
                ["ShortlistResponse — unlocked", "candidates[].{name, match_score, justification, gap_analysis?}", "matching-svc", "Phase 2–6"],
                ["AutoApplicationMatch", "request_id, candidate_id, scores, inclusion/exclusion reasons", "matching-svc", "Phase 1"],
                ["AssistantMessage", "{message, session_id} → SSE stream of tokens", "ai-assistant-svc", "Phase 6"],
                ["WebhookPayment", "HMAC-signed body — verified before any state change", "payment-svc", "Phase 2"],
                ["FounderQuotaStatus", "{professional_remaining, business_remaining, total_quotas}", "—", "Phase 1"],
            ],
        },
        "5. Error Handling": {
            "narrative": "Errors use standard HTTP status codes plus a structured error body. No stack traces or internal identifiers leak to clients.",
            "rows": [
                ["400 — validation", "{error: 'validation_failed', fields: {...}}", "Architect", "Standard"],
                ["401 — auth", "{error: 'unauthenticated'} — no detail", "Architect", "Standard"],
                ["403 — forbidden", "{error: 'forbidden'} — no detail", "Architect", "Standard"],
                ["404 — not found", "{error: 'not_found'}", "Architect", "Standard"],
                ["409 — conflict", "{error: 'conflict', reason: '...'} — e.g., duplicate referral code", "Architect", "Standard"],
                ["410 — gone", "Internal portal closed", "Architect", "Standard"],
                ["422 — semantic", "{error: 'unprocessable', detail: '...'} — e.g., weights don't sum to 100", "Architect", "Standard"],
                ["423 — locked", "Portal locked at closing time", "Architect", "Standard"],
                ["429 — rate limit", "{error: 'rate_limited', retry_after_seconds: N}", "Architect", "Standard"],
                ["500 — server error", "{error: 'internal_error', correlation_id: 'uuid'} — correlation id matches Tempo trace", "Architect", "Standard"],
            ],
        },
        "6. Rate Limiting": {
            "narrative": "Rate limits enforced at the API gateway. Limits per spec §17.1.",
            "rows": [
                ["Authenticated user", "100 requests/minute", "Gateway", "Mandatory"],
                ["Auth endpoints", "10 requests/minute (proxied to Keycloak)", "Gateway", "Mandatory"],
                ["Public portal endpoint", "5 submissions/IP/hour", "Gateway", "Mandatory"],
                ["AI Assistant", "10 messages/minute/user; 5/minute/IP public; 30 messages/session", "ai-assistant-svc", "Mandatory"],
                ["Webhooks", "No client rate limit — gateway accepts, validates HMAC", "Gateway", "Standard"],
            ],
        },
        "7. API Security": {
            "narrative": "API-layer security controls. Detailed in ILLM-03-007.",
            "rows": [
                ["TLS", "1.2 minimum; 1.3 preferred; HSTS enforced", "Gateway", "Mandatory"],
                ["Auth", "Keycloak Bearer tokens; gateway validates signature, expiry, scope, audience", "Gateway", "Mandatory"],
                ["Scope/audience", "Token scoped to portal — Professional token cannot call Admin endpoints", "Gateway", "Mandatory"],
                ["CORS", "Allow-list origins per environment; credentials supported for portal SPAs", "Gateway", "Standard"],
                ["CSRF", "SameSite=Strict cookies for SPA sessions; double-submit token for mutations", "Architect", "Standard"],
                ["Input validation", "Server-side schema validation; never trust client headers", "Architect", "Mandatory"],
                ["File upload", "MIME enforced server-side; ClamAV scan async on upload event", "Architect", "Mandatory"],
                ["Webhook HMAC", "Verified before any state change; tampered/replayed payloads rejected", "payment-svc", "Mandatory"],
            ],
        },
        "8. Versioning Strategy": {
            "narrative": "API versioning lives in the URI path. Breaking changes increment the major version; non-breaking additions extend within the current version.",
            "rows": [
                ["Initial version", "/api/v1 — Phase 1 launch", "Architect", "Confirmed"],
                ["Additive changes", "New endpoints, new optional fields — same version", "Architect", "Standard"],
                ["Breaking changes", "New version (v2) with parallel runtime; v1 deprecated with 6-month notice", "Architect", "Standard"],
                ["Deprecation header", "Sunset header per RFC 8594 on deprecated endpoints", "Architect", "Standard"],
                ["OpenAPI spec snapshot", "Tagged in source repo per release; published via Swagger UI / Redoc", "DevOps", "Standard"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-03-007 — Security Design
# ════════════════════════════════════════════════════════════════════════════
BATCH2["03_System_Design/Detailed_Design/Security_Design/ILLM-03-007_Security_Design_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §17.1 and §35. Keycloak-centric IAM model; ISO/IEC 27001 control alignment; Section 31 branding policy enforcement.",
    "sections": {
        "2. Security Architecture": {
            "narrative": "Security is layered. The platform delegates identity and authentication to Keycloak; authorisation is enforced at the API gateway and re-checked per service. Secrets live in HashiCorp Vault. Uploaded files are scanned by ClamAV. Sensitive database fields are encrypted with AES-256 (pgcrypto). All payment flows are PCI-DSS SAQ A through hosted gateway pages — no card data ever touches the platform. Audit logs and compliance justifications are append-only with 7-year retention.",
            "rows": [
                ["Identity authority", "Keycloak — OIDC/OAuth2; MFA via TOTP/WebAuthn", "DevOps", "Confirmed"],
                ["Authorisation enforcement", "API gateway validates token; each microservice re-checks scopes", "Architect", "Mandatory"],
                ["Secret management", "HashiCorp Vault (OSS) — DB creds, API keys, webhook secrets", "DevOps", "Confirmed"],
                ["Virus scanning", "ClamAV on every uploaded file before further processing", "DevOps", "Confirmed"],
                ["Encryption at rest", "AES-256 via pgcrypto for id_number, student_number", "Architect", "Confirmed"],
                ["Encryption in transit", "TLS 1.2+ minimum; HSTS; 1.3 preferred", "Architect", "Confirmed"],
                ["PCI-DSS scope", "SAQ A only — hosted gateway model", "Compliance", "Confirmed"],
                ["Audit immutability", "audit_logs and compliance_justifications append-only; no UI delete", "Architect", "Mandatory"],
            ],
        },
        "3. Authentication and Authorisation": {
            "narrative": "Keycloak owns the identity layer. The platform's users table holds only profile information keyed off Keycloak's sub claim. RBAC is enforced via Keycloak realms and roles.",
            "rows": [
                ["Identity provider", "Keycloak — single source of authentication for all five portals", "DevOps", "Confirmed"],
                ["Protocols", "OIDC + OAuth2 — Authorization Code with PKCE for portal SPAs", "Architect", "Confirmed"],
                ["Token TTL", "Access 15min; refresh 30d (Keycloak-managed)", "Architect", "Confirmed"],
                ["MFA", "TOTP and WebAuthn supported; mandatory for admin and support realms", "Architect", "Mandatory"],
                ["Password storage", "Owned by Keycloak (argon2id); platform never sees or stores passwords", "Architect", "Mandatory"],
                ["Federation", "Optional social login via Google/Microsoft through Keycloak providers", "DevOps", "Optional"],
                ["RBAC", "Realms per portal type; roles mapped to platform identifiers (job_seeker, employer, admin, support, super_admin)", "Architect", "Confirmed"],
                ["Scope enforcement", "API gateway rejects tokens lacking required scope before reaching service", "Architect", "Mandatory"],
            ],
        },
        "4. Encryption Strategy": {
            "narrative": "Encryption applied at three layers: in transit (TLS), at rest (database column-level + storage-level), and field-level for highly sensitive identifiers.",
            "rows": [
                ["In transit — external", "TLS 1.2 minimum (1.3 preferred), HSTS preloaded, automated certificate renewal", "DevOps", "Mandatory"],
                ["In transit — internal mesh", "mTLS between microservices in K8s (cert-manager + Istio or similar)", "DevOps", "Recommended"],
                ["At rest — storage", "Encrypted block storage for DB and MinIO", "DevOps", "Mandatory"],
                ["At rest — column level", "AES-256 via pgcrypto for id_number, student_number", "Architect", "Mandatory"],
                ["Passwords", "Hashed by Keycloak (argon2id) — never reversible", "Architect", "Mandatory"],
                ["Secrets in transit", "Vault-issued short-lived credentials; rotated daily", "DevOps", "Recommended"],
                ["Webhook payload", "HMAC-signed; verified before processing", "Architect", "Mandatory"],
                ["Backups", "Encrypted at rest; encrypted in transit during replication", "DevOps", "Mandatory"],
            ],
        },
        "5. Security Controls": {
            "narrative": "Controls mapped to ISO/IEC 27001 Annex A families.",
            "rows": [
                ["A.5 Information security policies", "Documented in folder 09 Compliance/Legal; reviewed annually", "Compliance", "Mapped"],
                ["A.8 Asset management", "Configuration Items Register (ILLM-14-002); Infrastructure Inventory (ILLM-14-004)", "Operations", "Mapped"],
                ["A.9 Access control", "Keycloak RBAC; API gateway enforcement; least privilege per role", "Architect", "Mapped"],
                ["A.10 Cryptography", "Encryption strategy §4 above", "Architect", "Mapped"],
                ["A.12 Operations security", "Patch management, change control, capacity planning per Ops folder", "DevOps", "Mapped"],
                ["A.13 Communications security", "TLS, mTLS, segmentation, gateway-enforced ingress", "DevOps", "Mapped"],
                ["A.14 System acquisition / dev / maintenance", "Coding Standards (ILLM-07-001); secure development lifecycle", "Architect", "Mapped"],
                ["A.16 Incident management", "Incident Management runbook (ILLM-11-004); on-call rotation", "DevOps", "Mapped"],
                ["A.17 BCP", "Backup/Recovery (ILLM-11-003); rollback (ILLM-10-006)", "DevOps", "Mapped"],
                ["A.18 Compliance", "Labour Act, ETA, PCI-DSS, Section 31 branding policy", "Compliance", "Mapped"],
            ],
        },
        "6. PCI-DSS Compliance": {
            "narrative": "PCI-DSS scope is restricted to SAQ A through the hosted-gateway payment model. No card data is captured, transmitted, processed, or stored on Illumin360 infrastructure.",
            "rows": [
                ["Scope", "SAQ A only", "Compliance", "Confirmed"],
                ["Card data flow", "Customer enters card directly on gateway hosted page; never touches platform", "Architect", "Confirmed"],
                ["Webhook contents", "Token reference, status, amount; no PAN", "Architect", "Confirmed"],
                ["TLS for redirect", "TLS 1.2+ enforced on redirect endpoint", "DevOps", "Confirmed"],
                ["Quarterly ASV scan", "Required by PCI-DSS SAQ A — scheduled in Operations calendar", "DevOps", "Required"],
                ["SAQ A attestation", "Submitted annually to acquirer", "Compliance", "Required"],
            ],
        },
        "7. Penetration Test Requirements": {
            "narrative": "Penetration testing required pre-launch and annually thereafter, plus after major architecture changes.",
            "rows": [
                ["Pre-launch test", "External pentest covering OWASP Top 10; web app + API surface", "Security vendor", "Required pre-Phase 1 go-live"],
                ["Internal DAST", "OWASP ZAP automated scan in CI pipeline", "QA", "Continuous"],
                ["Container scan", "Trivy in CI on every image build", "DevOps", "Continuous"],
                ["Dependency scan", "Dependabot / Renovate equivalent on every PR", "DevOps", "Continuous"],
                ["Annual test", "Repeat external pentest; remediation tracked to closure", "Security vendor", "Annual"],
                ["Targeted tests", "After AI integration changes, IAM changes, payment changes", "Security vendor", "Event-driven"],
            ],
        },
        "8. Review and Approval": {
            "narrative": "Security design sign-off.",
            "rows": [
                ["Architect", "Security architecture and controls reviewed", "Architect", "Drafted"],
                ["Compliance", "ISO 27001 control mapping verified", "Compliance", "Pending"],
                ["External auditor", "Pre-launch security review", "Auditor", "Pending"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-03-008 — AI Services Design
# ════════════════════════════════════════════════════════════════════════════
BATCH2["03_System_Design/Detailed_Design/AI_Services_Design/ILLM-03-008_AI_Services_Design_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §28 and §29. Internal technical document — vendor names permitted here per §31 (developer integration docs exempt from branding policy). Client-facing artefacts must continue to use Illumin360 branding only.",
    "sections": {
        "2. AI Services Overview": {
            "narrative": "Three AI-powered workloads run on Anthropic Claude Sonnet 4.6: structured CV analysis, justification narrative generation, and the Illumin360 Platform Assistant. OCR for scanned CVs uses Google Cloud Vision (with Tesseract local fallback). All AI services are accessed via pay-per-use APIs — no infrastructure built or maintained in-house. Section 31 branding policy mandates these vendor names never appear in client-facing artefacts.",
            "rows": [
                ["CV analysis", "Anthropic Claude Sonnet 4.6 — structured extraction", "professional-svc", "Phase 1"],
                ["Justification narrative", "Anthropic Claude Sonnet 4.6 — 3–6 sentence per shortlisted candidate", "matching-svc", "Phase 2"],
                ["Platform Assistant", "Anthropic Claude Sonnet 4.6 — 5 per-user-type instances; streaming", "ai-assistant-svc", "Phase 6"],
                ["OCR for scanned CVs", "Google Cloud Vision DOCUMENT_TEXT_DETECTION", "professional-svc", "Phase 1"],
                ["OCR fallback", "Tesseract (local) when Cloud Vision unavailable", "professional-svc", "Phase 1"],
                ["Document parsing", "Apache Tika — text extraction from PDF/DOCX before AI", "professional-svc", "Phase 1"],
                ["Optional self-hosted fallback", "Ollama — Phase 8 evaluation", "ai-assistant-svc", "Phase 8 (option)"],
                ["Branding compliance", "Section 31 — vendor names never in client-facing content", "Marketing", "Mandatory"],
            ],
        },
        "3. Claude Sonnet Configuration": {
            "narrative": "Configuration parameters and runtime contract with Anthropic API.",
            "rows": [
                ["Model string", "claude-sonnet-4-6 — pinned exact; never use aliases in production", "Architect", "Mandatory"],
                ["Provider", "Anthropic — platform.anthropic.com", "—", "Confirmed"],
                ["Pricing", "USD 3.00 / M input tokens; USD 15.00 / M output", "Finance", "Current"],
                ["Batch API", "50% discount — use for all async CV processing", "Architect", "Configured"],
                ["Max output tokens", "4,096 per CV analysis request", "Architect", "Configured"],
                ["Timeout", "30 seconds; one retry on timeout; admin alert on second failure", "Architect", "Configured"],
                ["Streaming", "Enabled for AI Platform Assistant; not used for CV analysis", "Architect", "Configured"],
                ["Prompt caching", "Used for assistant system prompts — 90% cost reduction", "Architect", "Configured"],
                ["Authentication", "API key in HashiCorp Vault; injected at deploy time", "DevOps", "Required"],
                ["Privacy", "CV text and prompt context sent to Anthropic; disclosed in D-05", "Legal", "Documented"],
            ],
        },
        "4. Google Cloud Vision Configuration": {
            "narrative": "Cloud Vision usage limited to scanned-document OCR fallback.",
            "rows": [
                ["Feature", "DOCUMENT_TEXT_DETECTION — full-page layout extraction", "Architect", "Confirmed"],
                ["Trigger", "Standard extraction yields < 50 words or non-alphanumeric ratio high", "Architect", "Designed"],
                ["Authentication", "GCP service account JSON in Vault", "DevOps", "Required"],
                ["Pricing", "Free tier first 1,000 pages/month; USD 1.50 per 1,000 thereafter", "Finance", "Current"],
                ["Page cap per document", "First 5 pages; admin review flag if exceeded", "Architect", "Designed"],
                ["Confidence threshold", "< 80% triggers candidate to review CV upload quality", "Architect", "Designed"],
                ["Timeout", "60 seconds for multi-page documents", "Architect", "Configured"],
                ["Languages supported", "60+ including English and Afrikaans", "—", "Confirmed"],
            ],
        },
        "5. AI Processing Pipeline": {
            "narrative": "Combined pipeline for CV upload through structured data extraction. Async via RabbitMQ — never in the API request cycle.",
            "rows": [
                ["1. Upload", "Direct to MinIO via presigned URL; ClamAV virus scan async", "professional-svc", "Sync"],
                ["2. Standard extraction", "Apache Tika — text from PDF/DOCX", "professional-svc", "Sync"],
                ["3a. OCR (if needed)", "Google Cloud Vision — if standard extraction < 50 words", "professional-svc → Cloud Vision", "Async via queue"],
                ["3b. Local OCR fallback", "Tesseract — if Cloud Vision unavailable", "professional-svc", "Async via queue"],
                ["4. AI analysis", "Claude Sonnet 4.6 — structured JSON of skills, qualifications, experience, languages, NQF level", "professional-svc → Anthropic", "Async via queue"],
                ["5. Persist structured data", "Insert into candidate_profiles, candidate_skills, candidate_languages, candidate_qualifications", "professional-svc", "Sync"],
                ["6. Update profile completion", "Recompute profile_complete_pct; check badge triggers", "professional-svc", "Sync"],
                ["7. Notify candidate", "Email — CV processed and profile updated", "notification-svc", "Async"],
            ],
        },
        "6. AI Cost Model": {
            "narrative": "Cost projections per spec §29.4 with monitoring thresholds.",
            "rows": [
                ["Year 1 — CV analysis", "≈ USD 0.75/month (500 CVs, batch pricing)", "Finance", "Projected"],
                ["Year 1 — Justification", "≈ USD 0.50/month (80 shortlisted)", "Finance", "Projected"],
                ["Year 1 — Assistant", "≈ USD 3.00/month (200 conversations)", "Finance", "Projected"],
                ["Year 1 — OCR", "USD 0.00 (free tier)", "Finance", "Projected"],
                ["Year 1 — Total", "≈ NAD 77/month", "Finance", "Projected"],
                ["Year 3 — Total", "≈ NAD 1,116/month at projected scale (0.19% of revenue)", "Finance", "Projected"],
                ["Monitoring", "Daily token usage by workload; admin dashboard widget", "DevOps", "Required"],
                ["Alerting", "Trigger if monthly spend > 200% of same-month prior year", "DevOps", "Required"],
            ],
        },
        "7. Fallback Strategy": {
            "narrative": "Each AI service has a documented fallback path; failures observable via Grafana with on-call response.",
            "rows": [
                ["Claude CV analysis down", "Fall back to keyword matching; log fallback; queue retry", "Architect", "Designed"],
                ["Claude justification down", "Template-assembled justification; report flagged 'standard methodology'", "Architect", "Designed"],
                ["Claude Assistant down", "User-facing banner; user can escalate to Support; conversation suspended", "Architect", "Designed"],
                ["Google Vision down", "Tesseract local OCR; alert if confidence < 80%; admin notified", "Architect", "Designed"],
                ["Multi-vendor outage", "Phase 8 Ollama self-hosted fallback for Assistant (optional)", "Architect", "Future"],
            ],
        },
        "8. AI Privacy Controls": {
            "narrative": "Privacy disclosures and consent gates for AI processing.",
            "rows": [
                ["Disclosure in D-05", "Candidate consent explicitly mentions automated processing", "Legal", "Mandatory"],
                ["Disclosure in D-07", "Internal portal applicants notified of automated processing", "Legal", "Mandatory"],
                ["Disclosure in D-02", "Reports state automated generation via 'Illumin360 proprietary matching system' (v3.7 wording)", "Legal", "Mandatory"],
                ["Branding policy compliance", "Assistant never identifies as Claude; post-gen filter rejects responses with prohibited terms", "Architect", "Mandatory"],
                ["No PII in prompts beyond what's necessary", "Strip identifiers where not required; minimise data sent to Anthropic", "Architect", "Standard"],
                ["No CV photos sent to AI", "Photo never passed to matching engine or justification engine — blind screening", "Architect", "Mandatory"],
                ["Logging", "Inputs and outputs to AI services logged (truncated for PII) in ai_processing_log", "Architect", "Designed"],
                ["Data residency", "Anthropic and Google may process outside Namibia; documented and consented", "Legal", "Disclosed"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-12-007 — Sales Talking Points
# ════════════════════════════════════════════════════════════════════════════
BATCH2["12_Documentation/Sales_Marketing/Sales_Talking_Points/ILLM-12-007_Sales_Talking_Points_v1_0.docx"] = {
    "v2_change_description": "Populated from the existing Illumin360_Sales_Talking_Points.docx in the Draft Concept folder, refined per Section 31 branding policy and the v3.7 product model (Professional / Business / Student terminology).",
    "sections": {
        "2. Purpose": {
            "narrative": "This document provides sales-conversation talking points for the Illumin360 platform. The audience is Illumin sales staff and partners engaging Businesses considering the platform vs traditional recruitment agencies. Content aligns with Section 31 branding policy — never name AI vendors or underlying technology to prospects.",
            "rows": [
                ["Purpose", "Equip sales conversations with consistent talking points", "Marketing", "Active"],
                ["Audience — internal", "Sales staff, account managers, partners", "Marketing", "—"],
                ["Audience — external", "Business prospects evaluating Illumin360", "Marketing", "—"],
                ["Branding compliance", "Section 31 — Illumin360 branding only, no AI/vendor mentions", "Marketing", "Mandatory"],
            ],
        },
        "3. Background": {
            "narrative": "The Namibian recruitment market is dominated by agencies charging 15–25% of annual salary per hire on a per-vacancy basis. Internal vacancies are managed manually. Pipeline talent (students approaching graduation) is largely invisible. Illumin360 addresses all three with a flat-fee subscription-and-per-request platform.",
            "rows": [
                ["Market context", "Agency-dominated recruitment with per-hire commissions of 15–25%", "Marketing", "Reference"],
                ["Cost pain", "NAD 30,000–80,000 per professional hire", "Marketing", "Reference"],
                ["Speed pain", "1–3 weeks per shortlist", "Marketing", "Reference"],
                ["Transparency pain", "Opaque selection — no audit trail", "Marketing", "Reference"],
                ["Internal recruitment pain", "Manually managed; no audit", "Marketing", "Reference"],
                ["Pipeline pain", "No channel to scout students before graduation", "Marketing", "Reference"],
            ],
        },
        "4. Cost-Benefit Analysis": {
            "narrative": "Side-by-side comparison with traditional recruitment agencies, framed as the cost argument, the speed argument, and the transparency argument.",
            "rows": [
                ["Cost — agency", "15–25% of annual salary; NAD 30,000–50,000 on a NAD 200,000 role", "—", "Reference"],
                ["Cost — Illumin360", "NAD 1,725 flat fee per shortlist report — every time, for any role", "—", "Reference"],
                ["Savings example", "Single mid-level hire saving: NAD 28,000–98,000", "—", "Reference"],
                ["Speed — agency", "1–3 weeks per shortlist for standard roles", "—", "Reference"],
                ["Speed — Illumin360", "Shortlist generated in hours from request submission", "—", "Reference"],
                ["Transparency — agency", "Black-box selection; no scoring visible", "—", "Reference"],
                ["Transparency — Illumin360", "Match scores 0–100, sub-scores per factor, written analysis per shortlisted candidate", "—", "Reference"],
                ["Auto-Application advantage", "Candidates surfaced without applying — Business sees top-tier talent that wouldn't otherwise apply", "—", "Differentiator"],
                ["Internal recruitment", "Private branded portal — same matching quality applied to your own staff", "—", "Differentiator"],
                ["Graduate pipeline", "Students at UNAM/NUST/IUM discoverable before they hit the open market", "—", "Differentiator"],
            ],
        },
        "5. Objectives": {
            "narrative": "Sales objectives per conversation. Drives quarterly sales planning and partner enablement.",
            "rows": [
                ["Land cost argument", "Establish NAD 1,725 vs NAD 30,000–80,000 per hire", "Sales", "Primary"],
                ["Land speed argument", "Hours vs 1–3 weeks", "Sales", "Primary"],
                ["Land transparency argument", "Methodology disclosure vs black-box agency selection", "Sales", "Primary"],
                ["Land Auto-Application advantage", "No-apply-required talent surfacing", "Sales", "Differentiator"],
                ["Land internal recruitment value", "Same quality applied to internal vacancies", "Sales", "Differentiator"],
                ["Land graduate pipeline value", "First-mover access to Namibian student talent", "Sales", "Differentiator"],
                ["Position alongside agencies", "Use Illumin360 for professional/mid-level; agency for senior executive", "Sales", "Honest framing"],
                ["Subscription upgrade path", "Free → Starter → Growth → Enterprise — surface allowances and discounts", "Sales", "Conversion"],
            ],
        },
        "6. Recommendation": {
            "narrative": "Recommended sales playbook structure for first conversations and follow-ups.",
            "rows": [
                ["First conversation focus", "Cost, speed, transparency — the three arguments that always land", "Sales", "Playbook"],
                ["Objection — 'we have our agency'", "Position complementary — Illumin360 for professional roles, agency for senior exec", "Sales", "Playbook"],
                ["Objection — 'quality concern'", "Profile self-selection + university partnerships + visible profile data", "Sales", "Playbook"],
                ["Objection — 'replace human recruiter?'", "We handle search/ranking — interview judgement remains with you", "Sales", "Playbook"],
                ["Objection — 'what if we don't find candidates?'", "Combined search mode includes their own uploaded CVs", "Sales", "Playbook"],
                ["Honest positioning", "Illumin360 handles search/shortlisting — not reference checks, salary negotiation, or onboarding", "Sales", "Mandatory"],
                ["Call to action", "Run your first request — one vacancy, one shortlist, see the difference", "Sales", "Playbook"],
            ],
        },
        "7. Review and Approval": {
            "narrative": "Sales material review cadence.",
            "rows": [
                ["Marketing review", "Talking points reviewed quarterly for accuracy and tone", "Marketing", "Quarterly"],
                ["Compliance review", "Annual review for branding policy compliance (Section 31)", "Compliance", "Annual"],
                ["Sponsor approval", "Annual sign-off on positioning and pricing claims", "Sponsor", "Annual"],
            ],
        },
    },
}
