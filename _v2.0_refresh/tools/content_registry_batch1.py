"""
Batch-1 content for the populator — additional documents beyond Business Case and FR.
Imported and merged into REGISTRY by run_batch1.py.
"""

BATCH1 = {}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-02-001 — Business Requirements
# ════════════════════════════════════════════════════════════════════════════
BATCH1["02_Requirements/Business_Requirements/ILLM-02-001_Business_Requirements_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec. Captures the business goals and outcomes the platform must deliver across three customer segments and five portals.",
    "sections": {
        "2. Business Goals": {
            "narrative": "The platform is required to deliver three outcomes — a recurring-revenue subscription business serving Professionals, Students, and Businesses; a defensible recruitment process replacing the agency model for mid-tier roles in Namibia; and a scalable open-source-based architecture supporting eventual regional expansion.",
            "rows": [
                ["BG-1 Subscription revenue", "Recurring subscription revenue from all three customer segments", "Sponsor", "Approved"],
                ["BG-2 Replace agency cost", "Per-shortlist flat fee (NAD 1,725) replacing 15–25% agency placement fees", "Sponsor", "Approved"],
                ["BG-3 Defensible AI shortlist", "Transparent, auditable shortlist with methodology disclosure for legal defensibility", "Architect", "Approved"],
                ["BG-4 No missed opportunities", "Auto-Application Engine — candidates surfaced without applying", "Architect", "Approved"],
                ["BG-5 Student CSR pipeline", "Free student tier to build graduate-employer pipeline", "Sponsor", "Approved"],
                ["BG-6 Regional expansion path", "Architecture and standards alignment supporting future multi-country deployment", "Architect", "Approved"],
            ],
        },
        "3. Stakeholder Needs": {
            "narrative": "Stakeholder needs are captured by segment. Each is traced through to functional requirements (ILLM-02-002).",
            "rows": [
                ["Professionals", "Passive discoverability; benchmarking; gamified skill progression; subscription value", "FR-2, FR-24, FR-25", "Captured"],
                ["Students", "Scouting for graduate programmes; institution ranking; free tier", "FR-12, FR-24, FR-25", "Captured"],
                ["Businesses (general)", "Pre-curated talent pool; cost vs agency; speed; transparency", "FR-4, FR-5, FR-6", "Captured"],
                ["Businesses (talent acquisition)", "Top-tier candidates surfaced without candidate applying", "FR-24", "Captured"],
                ["Businesses (benchmarking)", "Workforce vs industry; why losing talent; skill gaps", "FR-25", "Captured"],
                ["Businesses (graduate programmes)", "Targeting top students before market entry", "FR-12, FR-25", "Captured"],
                ["Administrators", "Full oversight; compliance reviews; analytics; refund control", "FR-10", "Captured"],
                ["Support staff", "Ticket triage; identity verification; KB authoring; abuse moderation", "FR-23", "Captured"],
            ],
        },
        "4. Success Criteria": {
            "narrative": "Success criteria define the measurable outcomes the platform must hit. Tracked in the Status Reports (ILLM-13-004).",
            "rows": [
                ["SC-1 Founder quota fill", "First 300 Professional + 50 Business Founder slots claimed within 90 days of launch", "Marketing", "Target"],
                ["SC-2 Shortlist time", "P95 shortlist generation under 30 seconds for pools up to 5,000", "Architect", "Target"],
                ["SC-3 Subscription retention", "Year 1 Professional retention ≥ 70% at end of term", "Marketing", "Target"],
                ["SC-4 Business per-request volume", "Average 2+ requests per active Business per quarter by month 12", "Marketing", "Target"],
                ["SC-5 Internal portal adoption", "≥ 20% of Business accounts use internal recruitment within Year 1", "Marketing", "Target"],
                ["SC-6 Student verification", "≥ 80% automatic verification rate via institutional email domains", "Architect", "Target"],
                ["SC-7 AI services cost ratio", "AI services cost stays under 1% of revenue at every quarter", "Finance", "Target"],
            ],
        },
        "5. Constraints": {
            "narrative": "Constraints bound the solution space. Compliance constraints are non-negotiable.",
            "rows": [
                ["Jurisdiction", "Namibia — Labour Act 11 of 2007, Electronic Transactions Act 4 of 2019, Constitution Article 10", "Legal", "Mandatory"],
                ["PCI-DSS", "SAQ A only — no card data stored on platform", "Architect", "Mandatory"],
                ["Branding policy", "Section 31 — no client-facing AI/vendor references", "Marketing", "Mandatory"],
                ["Blind-screening", "Candidate photos not in matching engine or shortlist projection", "Architect", "Mandatory"],
                ["Standards alignment", "ISO/IEC 12207, 81346, 9241, 27001; OpenAPI; DDD/Clean/Microservices/REST/.NET", "Architect", "Required"],
                ["Open-source preference", "Runtime stack to be OSS where possible; AI services external", "Architect", "Preferred"],
            ],
        },
        "6. Assumptions": {
            "narrative": "Assumptions captured for explicit review during planning.",
            "rows": [
                ["Demand", "Sufficient Professional and Business demand in Namibia to reach Founder quotas", "Sponsor", "Assumed"],
                ["University partnerships", "UNAM, NUST, IUM agreeable to institutional verification programme", "Marketing", "Assumed"],
                ["Payment gateway", "Local Namibian gateway available with hosted-page integration", "Architect", "Assumed"],
                ["AI vendor stability", "Anthropic Claude Sonnet 4.6 remains available and pricing stable", "Architect", "Assumed"],
                ["Connectivity", "Sufficient bandwidth across Namibia for PWA-quality experience", "Architect", "Assumed"],
            ],
        },
        "7. Dependencies": {
            "narrative": "External dependencies the platform relies on at launch.",
            "rows": [
                ["Keycloak", "OIDC IAM authority", "DevOps", "Required"],
                ["RabbitMQ", "Async message queue", "DevOps", "Required"],
                ["PostgreSQL 15+", "Primary data store", "DevOps", "Required"],
                ["MinIO or S3-compatible storage", "Object storage for CVs/photos/videos/reports", "DevOps", "Required"],
                ["Anthropic API", "Claude Sonnet 4.6 for CV analysis and justification engine", "Architect", "Required"],
                ["Google Cloud Vision (or Tesseract fallback)", "OCR for scanned CVs", "Architect", "Required"],
                ["Payment gateway", "TBD — Namibian provider supporting hosted page model", "Architect", "Required"],
                ["Email provider", "Postal/Postfix or SaaS for transactional email", "DevOps", "Required"],
                ["ClamAV", "Virus scanning for uploaded files", "DevOps", "Required"],
            ],
        },
        "8. Approval": {
            "narrative": "Approval of this business requirements specification authorises the FR and design teams to proceed.",
            "rows": [
                ["Project Sponsor", "Business goals and success criteria approved", "CEO Illumin Investments CC", "Pending"],
                ["Marketing Lead", "Stakeholder-need framing approved", "Marketing", "Pending"],
                ["Architect", "Constraints and dependencies feasible", "Software Engineer & Architect", "Drafted"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-02-004 — Use Cases / User Stories
# ════════════════════════════════════════════════════════════════════════════
BATCH1["02_Requirements/Use_Cases_User_Stories/ILLM-02-004_Use_Cases_User_Stories_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec. Use cases per segment plus cross-cutting flows (Auto-Application, Founder grant, AI Assistant escalation).",
    "sections": {
        "2. Actors": {
            "narrative": "Actors map to the five portal roles plus external systems.",
            "rows": [
                ["Professional", "Subscribed individual seeking passive discoverability", "Primary actor", "—"],
                ["Student", "Enrolled student with free CSR tier", "Primary actor", "—"],
                ["Business", "Subscribed organisation searching for talent", "Primary actor", "—"],
                ["Administrator", "Platform operator", "Secondary actor", "—"],
                ["Support staff", "Customer support", "Secondary actor", "—"],
                ["AI Assistant", "Conversational support agent (system)", "System actor", "—"],
                ["Payment gateway", "Hosted card payment", "External system", "—"],
                ["Keycloak", "Identity provider", "External system", "—"],
            ],
        },
        "3. Professional Use Cases": {
            "narrative": "Use cases owned by the Professional actor.",
            "rows": [
                ["UC-P-01 Register", "Sign up via Keycloak; consent to D-05 + Auto-Application; trigger Founder grant if quota open", "Phase 1", "Designed"],
                ["UC-P-02 Build profile", "Add skills, languages, qualifications, CV, optional photo and video", "Phase 1", "Designed"],
                ["UC-P-03 Manage subscription", "Choose plan; pay via gateway; auto-renew reminders", "Phase 1–2", "Designed"],
                ["UC-P-04 View dashboard", "Profile completion, badges, referral stats, benchmarking rank", "Phase 5", "Designed"],
                ["UC-P-05 Set Auto-Application preferences", "Industry/employer/request-type opt-outs; notification tier", "Phase 1", "Designed"],
                ["UC-P-06 View notifications", "Match notifications respecting tier preference", "Phase 2", "Designed"],
                ["UC-P-07 Refer a peer", "Generate referral code; track conversions; receive free month per converted referral", "Phase 5", "Designed"],
                ["UC-P-08 Use AI Assistant", "Ask platform questions; escalate to Support if needed", "Phase 6", "Designed"],
            ],
        },
        "4. Student Use Cases": {
            "narrative": "Use cases owned by the Student actor.",
            "rows": [
                ["UC-S-01 Register as student", "Verification by institutional email domain or manual admin review", "Phase 5", "Designed"],
                ["UC-S-02 Build student profile", "Programme, NQF level on completion, modules, achievements, internships", "Phase 5", "Designed"],
                ["UC-S-03 Graduate Spotlight nomination", "Consent flow for monthly spotlight feature", "Phase 5", "Designed"],
                ["UC-S-04 View leaderboard", "Institution and national ranking by programme", "Phase 5–6", "Designed"],
                ["UC-S-05 Graduate upgrade", "60-day grace period; convert to paid Professional plan; carry profile forward", "Phase 5", "Designed"],
            ],
        },
        "5. Business Use Cases": {
            "narrative": "Use cases owned by the Business actor.",
            "rows": [
                ["UC-B-01 Register Business", "Sign up; consent to terms; trigger Founding Partner grant if quota open", "Phase 1", "Designed"],
                ["UC-B-02 Choose subscription tier", "Free / Starter / Growth / Enterprise per §37", "Phase 2", "Designed"],
                ["UC-B-03 Create recruitment request", "Pick search mode; set filters; confirm D-03 declaration; submit", "Phase 1", "Designed"],
                ["UC-B-04 Set custom weights", "Adaptive weighting — within bounds; locked on submission", "Phase 6", "Designed"],
                ["UC-B-05 Review shortlist preview", "Anonymous preview with match scores; decide to unlock", "Phase 1", "Designed"],
                ["UC-B-06 Unlock shortlist report", "Pay; receive PDF + Word reports with methodology disclosure", "Phase 2", "Designed"],
                ["UC-B-07 Unlock candidate", "Per-candidate fee; full profile + CV + photo revealed", "Phase 3", "Designed"],
                ["UC-B-08 Create internal portal", "Private branded link; staff apply directly; consolidated billing", "Phase 4", "Designed"],
                ["UC-B-09 Workforce benchmarking", "Upload anonymised workforce; receive benchmarking report", "Phase 6", "Designed"],
                ["UC-B-10 Manage business profile", "Logo upload; billing details; team members", "Phase 1", "Designed"],
                ["UC-B-11 Provide match feedback", "14 days post-unlock; rate accuracy and justification quality", "Phase 6", "Designed"],
                ["UC-B-12 View Compliant Recruiter badge", "Awarded monthly per §26.2 criteria", "Phase 6", "Designed"],
            ],
        },
        "6. Administrator Use Cases": {
            "narrative": "Use cases owned by the Administrator actor.",
            "rows": [
                ["UC-A-01 Review compliance justifications", "Approve or reject sensitive-filter requests", "Phase 3", "Designed"],
                ["UC-A-02 Approve shortlist for sensitive-filtered requests", "Required before Business can unlock", "Phase 3", "Designed"],
                ["UC-A-03 Process refunds", "Initiated by Support; admin approves and triggers gateway refund", "Phase 3", "Designed"],
                ["UC-A-04 Manage pricing", "Update pricing_plans table for subscription and per-request prices", "Phase 3", "Designed"],
                ["UC-A-05 Manage notification templates", "Edit email and in-product notification copy", "Phase 3", "Designed"],
                ["UC-A-06 Override Founder grant", "Grant Founder status outside quota with documented reason", "Phase 1", "Designed"],
                ["UC-A-07 View audit logs", "Filterable by user, event type, date, entity", "Phase 3", "Designed"],
            ],
        },
        "7. Support Use Cases": {
            "narrative": "Use cases owned by the Support staff actor (Phase 5+).",
            "rows": [
                ["UC-SU-01 Triage ticket", "Classify, prioritise, assign", "Phase 5", "Designed"],
                ["UC-SU-02 Verify student manually", "Method 3 verification — review uploaded enrolment letter", "Phase 5", "Designed"],
                ["UC-SU-03 Author KB article", "Markdown editor; published article feeds public FAQ + AI Assistant", "Phase 5", "Designed"],
                ["UC-SU-04 Moderate abuse report", "Review flagged video / employer review / assistant transcript", "Phase 5", "Designed"],
                ["UC-SU-05 Handle escalated assistant conversation", "Receive full transcript + context; respond and resolve", "Phase 6", "Designed"],
            ],
        },
        "8. Cross-Cutting Use Cases": {
            "narrative": "Use cases that span multiple actors or services.",
            "rows": [
                ["UC-X-01 Auto-Application", "Candidate's active profile = standing application against every fitting request", "Phase 1", "Designed"],
                ["UC-X-02 Founder grant", "First 300 Professional / 50 Business registrations receive Founder badge atomically", "Phase 1", "Designed"],
                ["UC-X-03 Internal portal close-and-process", "Cron auto-closes portal at set time; matching runs; report generated", "Phase 4", "Designed"],
                ["UC-X-04 AI Assistant escalation", "User clicks escalate; transcript + context emailed and ticket created", "Phase 6", "Designed"],
                ["UC-X-05 Graduate upgrade", "Student transitions to paid Professional plan atomically; profile preserved", "Phase 5", "Designed"],
            ],
        },
        "9. Approval": {
            "narrative": "Approval of this use case specification authorises detailed design against the modelled flows.",
            "rows": [
                ["Architect", "Use case set is complete and covers all 5 portals", "Software Engineer & Architect", "Pending"],
                ["Product Owner", "Use cases reflect business intent", "Sponsor", "Pending"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-03-002 — Technology Stack
# ════════════════════════════════════════════════════════════════════════════
BATCH1["03_System_Design/High_Level_Design/Technology_Stack/ILLM-03-002_Technology_Stack_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §36. Open-source runtime stack confirmed: Keycloak, RabbitMQ, Grafana OSS. .NET implied as primary application language pending explicit confirmation.",
    "sections": {
        "2. Stack Overview": {
            "narrative": "The Illumin360 platform runs on a fully open-source runtime stack. The only non-OSS dependencies are the third-party AI services (Anthropic Claude Sonnet 4.6, Google Cloud Vision OCR) and the chosen payment gateway. Application code is in C# / ASP.NET Core (implied by §35 Microsoft .NET Naming Guidelines — to be explicitly confirmed).",
            "rows": [
                ["Runtime stack basis", "Open-source first", "Architect", "Confirmed"],
                ["Non-OSS components", "Anthropic API, Google Cloud Vision, payment gateway", "Architect", "Confirmed"],
                ["Application language", ".NET (C# / ASP.NET Core) — implied by standards list", "Architect", "Pending confirm"],
                ["Primary database", "PostgreSQL 15+", "Architect", "Confirmed"],
            ],
        },
        "3. Identity and Access": {
            "narrative": "Keycloak is the OIDC/OAuth2 authority for all five portals. The platform's users table is a profile shell keyed off Keycloak's sub claim — no passwords are stored on the platform.",
            "rows": [
                ["Identity provider", "Keycloak", "DevOps", "Confirmed"],
                ["Protocol", "OIDC / OAuth2", "Architect", "Confirmed"],
                ["MFA", "Keycloak built-in — TOTP + WebAuthn", "Architect", "Confirmed"],
                ["Federation", "Social login (Google/Microsoft) via Keycloak providers", "Architect", "Optional"],
                ["Role model", "Keycloak realms + roles mapped to platform identifiers (job_seeker, employer, admin, support)", "Architect", "Confirmed"],
            ],
        },
        "4. Application Layer": {
            "narrative": "Application services are organised by DDD bounded context. Each service is a separate deployment unit. See ILLM-03-001 Architecture Diagrams for the full bounded-context map.",
            "rows": [
                ["Language", ".NET (C# / ASP.NET Core) — pending confirmation", "Architect", "Indicative"],
                ["ORM", "Entity Framework Core or Dapper", "Architect", "Open"],
                ["Resilience", "Polly — retries, circuit breakers, timeouts", "Architect", "Recommended"],
                ["Logging", "Serilog → OpenTelemetry → Loki", "Architect", "Confirmed"],
                ["API style", "REST per OpenAPI 3.x spec", "Architect", "Confirmed"],
                ["Service-to-service", "Async via RabbitMQ for events; sync via REST via API gateway", "Architect", "Confirmed"],
            ],
        },
        "5. Data Layer": {
            "narrative": "Primary storage is PostgreSQL. Semantic search runs in-place via the pgvector extension. Object storage uses MinIO. Caching and session storage use Redis.",
            "rows": [
                ["Relational DB", "PostgreSQL 15+ — per illumin360_master_migrations_v3.6.sql", "Architect", "Confirmed"],
                ["Vector search", "pgvector extension — semantic similarity without separate vector DB", "Architect", "Confirmed"],
                ["Object storage", "MinIO (S3-compatible) — CVs, videos, photos, logos, reports", "Architect", "Confirmed"],
                ["Cache & session", "Redis", "Architect", "Confirmed"],
                ["Encryption at rest", "AES-256 for id_number, student_number (pgcrypto)", "Architect", "Confirmed"],
            ],
        },
        "6. Messaging and Workflows": {
            "narrative": "Async work flows via RabbitMQ. Long-running workflows orchestrated by Temporal.",
            "rows": [
                ["Message queue", "RabbitMQ — confirmed", "Architect", "Confirmed"],
                ["Workflow orchestration", "Temporal (OSS) — founder grant, portal close-and-process, subscription lifecycle", "Architect", "Recommended"],
                ["Cron jobs", "Per-minute (portal auto-close), daily 06:00 WAT (reminders, badges), monthly (compliant recruiter)", "Architect", "Confirmed"],
            ],
        },
        "7. Observability": {
            "narrative": "The Grafana LGTM stack — Grafana, Prometheus, Loki, Tempo — provides metrics, logs, and traces. OpenTelemetry is the instrumentation standard.",
            "rows": [
                ["Visualisation", "Grafana OSS — confirmed", "DevOps", "Confirmed"],
                ["Metrics", "Prometheus", "DevOps", "Recommended"],
                ["Logs", "Loki", "DevOps", "Recommended"],
                ["Traces", "Tempo", "DevOps", "Recommended"],
                ["Instrumentation", "OpenTelemetry", "DevOps", "Confirmed"],
                ["Load testing", "k6 (Grafana Labs)", "QA", "Recommended"],
            ],
        },
        "8. Edge and Gateway": {
            "narrative": "API gateway handles external traffic routing, OIDC token validation, and rate limiting. NGINX provides TLS termination and reverse-proxy duties.",
            "rows": [
                ["API gateway", "Kong (OSS) or Traefik", "Architect", "Recommended"],
                ["Reverse proxy", "NGINX", "DevOps", "Confirmed"],
                ["CDN", "Optional for public assets and PWA static files", "DevOps", "Optional"],
            ],
        },
        "9. Security Stack": {
            "narrative": "Security tooling complements Keycloak's IAM role.",
            "rows": [
                ["Virus scanning", "ClamAV — uploaded files", "DevOps", "Confirmed"],
                ["Secrets", "HashiCorp Vault (OSS)", "DevOps", "Recommended"],
                ["Container scanning", "Trivy in CI", "DevOps", "Recommended"],
                ["DAST", "OWASP ZAP in security test plan", "QA", "Recommended"],
            ],
        },
        "10. Document and Media Processing": {
            "narrative": "CV and document processing pipelines combine OSS tools and the third-party AI services.",
            "rows": [
                ["Document parsing", "Apache Tika — text extraction from PDF/DOCX", "Architect", "Recommended"],
                ["OCR fallback", "Tesseract OCR — local fallback when Google Vision unavailable", "Architect", "Recommended"],
                ["PDF generation", "WeasyPrint — shortlist report PDFs", "Architect", "Confirmed"],
                ["Word generation", "python-docx + docxtpl — shortlist report .docx", "Architect", "Confirmed"],
                ["Video transcription", "Provider-agnostic abstraction — Google Cloud Speech-to-Text or AWS Transcribe", "Architect", "Phase 7"],
            ],
        },
        "11. Container and Orchestration": {
            "narrative": "Containerised deployment on Kubernetes or K3s (lighter footprint).",
            "rows": [
                ["Container runtime", "Docker", "DevOps", "Confirmed"],
                ["Orchestration", "Kubernetes or K3s", "DevOps", "Recommended"],
                ["Helm", "Chart management", "DevOps", "Recommended"],
                ["GitOps", "Argo CD — declarative deployment", "DevOps", "Recommended"],
            ],
        },
        "12. Frontend": {
            "narrative": "Frontend stack to be finalised — the existing component library uses Tailwind-style utility classes. PWA layer covered in ILLM-03-018.",
            "rows": [
                ["CSS framework", "TailwindCSS — aligned with existing component library", "Architect", "Indicative"],
                ["Component framework", "TBD — React or Blazor consistent with .NET decision", "Architect", "Open"],
                ["PWA", "Manifest + service worker per ILLM-03-018", "Architect", "Confirmed"],
            ],
        },
        "13. AI Services (third-party)": {
            "narrative": "Per spec §28. These are the only non-OSS runtime dependencies. Section 31 branding policy ensures these names never appear in client-facing artefacts.",
            "rows": [
                ["CV analysis", "Anthropic Claude Sonnet 4.6 — claude-sonnet-4-6 (exact string)", "Architect", "Confirmed"],
                ["Justification engine", "Anthropic Claude Sonnet 4.6", "Architect", "Confirmed"],
                ["AI Platform Assistant", "Anthropic Claude Sonnet 4.6", "Architect", "Confirmed"],
                ["OCR for scanned docs", "Google Cloud Vision API — DOCUMENT_TEXT_DETECTION", "Architect", "Confirmed"],
                ["Self-hosted fallback (Phase 8 option)", "Ollama — local model for AI Assistant degraded mode", "Architect", "Optional"],
            ],
        },
        "14. Standards Compliance": {
            "narrative": "The stack supports compliance with the standards in spec §35. See ILLM-03-007 Security Design for ISO/IEC 27001 control mapping.",
            "rows": [
                ["ISO/IEC 27001", "Mapped via Security Design (ILLM-03-007); Keycloak + ClamAV + Vault + audit logs", "—", "Designed"],
                ["ISO 9241", "UI/UX Design (ILLM-03-006) — dialogue principles, human-centred design", "—", "Designed"],
                ["OpenAPI 3.x", "All REST endpoints specified; client SDKs generated from spec", "—", "Designed"],
                ["DDD + Clean + Microservices + REST", "Architecture Diagrams (ILLM-03-001)", "—", "Designed"],
                ["Microsoft .NET Naming Guidelines", "Coding Standards (ILLM-07-001) — pending .NET confirmation", "—", "Pending"],
            ],
        },
        "15. Approval": {
            "narrative": "Approval of this technology stack authorises the development team to procure, configure, and deploy the listed tools.",
            "rows": [
                ["Architect", "Stack is feasible and supports all FRs and NFRs", "Software Engineer & Architect", "Pending"],
                ["DevOps", "Stack is operable and within capacity budgets", "DevOps", "Pending"],
                ["Sponsor", "Stack costs aligned with cost-benefit analysis", "Sponsor", "Pending"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-06-001 — Phase 1 Core Talent Pool
# ════════════════════════════════════════════════════════════════════════════
BATCH1["06_Incremental_Delivery/Phase_1_Core_Talent_Pool/ILLM-06-001_Phase_1_Core_Talent_Pool_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec. Phase 1 scope includes core matching engine, Professional/Student/Business registration, Auto-Application Engine, and Founder Programme quota enforcement.",
    "sections": {
        "2. Phase Objectives": {
            "narrative": "Phase 1 delivers the core talent pool with end-to-end registration, profile management, recruitment requests, matching engine, anonymous shortlist preview, Auto-Application audit trail, and Founder Programme quota enforcement. Reporting and payments arrive in Phase 2; this phase establishes the platform's defining mechanics.",
            "rows": [
                ["O-1 Core registration", "Professional, Student, Business registration via Keycloak", "Architect", "In scope"],
                ["O-2 Profile management", "All segment-specific profile fields and CV upload", "Architect", "In scope"],
                ["O-3 Recruitment request", "Business creates request with 3 of 4 search modes (internal in Phase 4)", "Architect", "In scope"],
                ["O-4 Matching engine", "Two-pass matching with standard weights; 30s/5k target", "Architect", "In scope"],
                ["O-5 Anonymous shortlist preview", "Match scores visible; names/contacts hidden", "Architect", "In scope"],
                ["O-6 Auto-Application Engine", "Audit ledger per (request, candidate) consideration", "Architect", "In scope"],
                ["O-7 Founder Programme", "Quota-enforced grant of 300 Professional + 50 Business slots", "Architect", "In scope"],
                ["O-8 Compliance D-03/D-04", "Declaration and sensitive filter warning operational", "Architect", "In scope"],
            ],
        },
        "3. Phase Scope": {
            "narrative": "Detailed in/out-of-scope items for the Phase 1 build.",
            "rows": [
                ["In — services", "identity-svc, professional-svc, business-svc, recruitment-svc, matching-svc (core)", "Architect", "Confirmed"],
                ["In — portals", "Professional Portal, Student Portal, Business Portal (registration + core flows)", "Architect", "Confirmed"],
                ["In — Auto-Application", "Standing-application principle, opt-outs, audit ledger", "Architect", "Confirmed"],
                ["In — Founder", "Race-condition-safe grant logic", "Architect", "Confirmed"],
                ["Out — reporting", "PDF/Word generation moved to Phase 2", "Architect", "Out"],
                ["Out — payments", "Subscription billing, hosted gateway moved to Phase 2", "Architect", "Out"],
                ["Out — internal portal", "Phase 4", "Architect", "Out"],
                ["Out — Support Portal", "Phase 5", "Architect", "Out"],
                ["Out — AI Assistant", "Phase 6", "Architect", "Out"],
            ],
        },
        "4. Phase Deliverables": {
            "narrative": "Concrete deliverables of Phase 1 with sign-off owners.",
            "rows": [
                ["D-1 Database migrations 001–015", "All Phase 1 schema present and run in dev/staging/prod", "Architect", "Pending"],
                ["D-2 Keycloak realms", "professional, student, business, admin realms configured", "DevOps", "Pending"],
                ["D-3 Registration flows", "All three segments registering and authenticating", "QA", "Pending"],
                ["D-4 Profile management UI", "All segment-specific fields editable", "QA", "Pending"],
                ["D-5 Matching engine v1", "Two-pass, standard weights, 30s/5k target met", "Architect", "Pending"],
                ["D-6 Auto-Application ledger", "auto_application_matches populated correctly per request", "QA", "Pending"],
                ["D-7 Founder grant", "Quota race-tested under 50-concurrent-write load", "QA", "Pending"],
                ["D-8 Anonymous preview UI", "Match scores visible; PII hidden", "QA", "Pending"],
                ["D-9 Compliance D-03/D-04 enforcement", "Submission blocked without declaration; justification ≥50 words", "QA", "Pending"],
                ["D-10 OpenAPI spec for Phase 1 endpoints", "Auto-generated reference docs published", "Architect", "Pending"],
            ],
        },
        "5. Acceptance Criteria": {
            "narrative": "Phase 1 release gates. All criteria must pass before Phase 2 begins.",
            "rows": [
                ["A-1 Registration end-to-end", "All three segments can register, verify email, and log in", "QA Lead", "Pending"],
                ["A-2 Founder race condition", "Concurrent registrations resolve correctly; never two grants of slot 300", "QA Lead", "Pending"],
                ["A-3 Auto-Application audit", "Every request creates one auto_application_matches row per considered candidate", "QA Lead", "Pending"],
                ["A-4 Matching engine performance", "P95 ≤ 30 seconds for 5,000-candidate pool", "QA Lead", "Pending"],
                ["A-5 Anonymous preview privacy", "No PII leakage in preview; verified by API contract test", "QA Lead", "Pending"],
                ["A-6 D-03 enforcement", "Recruitment request submission blocked until declaration ticked", "QA Lead", "Pending"],
                ["A-7 D-04 sensitive filter flow", "Gender/age filter triggers warning + 50-word justification + admin alert", "QA Lead", "Pending"],
                ["A-8 Audit log immutability", "No UI path can delete an audit log row", "QA Lead", "Pending"],
            ],
        },
        "6. Dependencies": {
            "narrative": "Dependencies that must be in place before Phase 1 can complete.",
            "rows": [
                ["Keycloak deployed", "Realms, clients, role mappings configured", "DevOps", "Required"],
                ["PostgreSQL deployed", "v3.6 master migrations through Migration 015 applied", "DevOps", "Required"],
                ["MinIO deployed", "Buckets for CV upload provisioned", "DevOps", "Required"],
                ["RabbitMQ deployed", "Matching engine queue configured", "DevOps", "Required"],
                ["ClamAV deployed", "Virus scanning hooked into MinIO upload events", "DevOps", "Required"],
                ["Anthropic API key", "Provisioned and stored in Vault", "DevOps", "Required"],
            ],
        },
        "7. Risks": {
            "narrative": "Phase 1 specific risks. Cross-reference the global risk register (ILLM-13-007).",
            "rows": [
                ["Founder quota race", "Pure SELECT count is not safe — use SELECT FOR UPDATE serialisation", "Architect", "Mitigated by design"],
                ["Matching engine performance", "5k-candidate target requires query and index tuning", "Architect", "Tested in load runs"],
                ["Auto-Application volume", "Audit ledger writes scale with request × candidate — verify partitioning", "Architect", "To validate"],
                ["Keycloak misconfiguration", "Realm setup must match the application's expectations", "DevOps", "Spec'd and reviewed"],
            ],
        },
        "8. Increment Sign-Off": {
            "narrative": "Sign-off for Phase 1 transition to Phase 2.",
            "rows": [
                ["Architect", "All deliverables D-1 to D-10 met acceptance criteria", "Software Engineer & Architect", "Pending"],
                ["QA Lead", "Test pack executed and passed", "QA Lead", "Pending"],
                ["Product Owner", "Phase 1 demoed and accepted", "Sponsor", "Pending"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-09-002 — D-02 Report Disclaimer (v3.7 wording)
# ════════════════════════════════════════════════════════════════════════════
BATCH1["09_Compliance_Legal/Disclaimers/D02_Report_Disclaimer/ILLM-09-002_D02_Report_Disclaimer_v1_0.docx"] = {
    "v2_change_description": "Populated with v3.7 D-02 wording per spec §31.2 — 'Illumin360 proprietary matching system' replaces previous 'artificial intelligence' wording. Section 31 branding policy enforced.",
    "sections": {
        "2. Purpose": {
            "narrative": "D-02 is the disclaimer printed in the footer of every Illumin360 Shortlist Report (PDF and Word) and shown on the report preview screen. Its purpose is to make clear that the report is a decision-support tool generated by automated processing, that candidate data is self-declared and not verified by Illumin360, and that hiring decisions and due diligence remain the Business's responsibility.",
            "rows": [
                ["Purpose", "Limit Illumin360 liability and disclose automated processing for legal defensibility", "Legal", "Required"],
                ["Audience", "Every recipient of a shortlist report", "Marketing", "Confirmed"],
                ["Frequency", "Footer of every page; every PDF and Word report", "Architect", "Confirmed"],
            ],
        },
        "3. Disclaimer Text": {
            "narrative": "Verbatim disclaimer text per v3.7 spec §31.2. To be reproduced exactly in the report generator template.",
            "rows": [
                ["Heading line", "Illumin360 Shortlist Report Disclaimer", "Marketing", "Approved"],
                ["Body paragraph 1", "This report has been generated by the Illumin360 proprietary matching system using automated data processing. The rankings, match scores, and candidate analyses contained in this report are produced by an automated system and do not constitute professional recruitment advice, a recommendation to employ any specific individual, or an assessment of any candidate's character, reliability, or suitability beyond the documented criteria specified in the recruitment request.", "Legal", "Pending review"],
                ["Body paragraph 2", "The match scores and rankings are based solely on the structured profile data and CV content provided by candidates on the Illumin360 platform and are subject to the accuracy and completeness of that self-declared information. Illumin360 does not independently verify candidate qualifications, employment history, or any other information provided by candidates.", "Legal", "Pending review"],
                ["Body paragraph 3", "This report is provided as a decision-support tool only. The employer is solely responsible for conducting appropriate due diligence, including verification of qualifications and references, and for all hiring decisions made. Illumin accepts no liability for any loss, damage, or adverse outcome arising from reliance on this report.", "Legal", "Pending review"],
                ["Body paragraph 4", "This report is confidential and intended solely for the authorised recipient.", "Legal", "Pending review"],
                ["Attribution line", "Prepared by: Illumin360 │ Illumin Investments CC │ Windhoek, Namibia │ www.illumininvestments.com", "Marketing", "Confirmed"],
            ],
        },
        "4. Implementation Location": {
            "narrative": "D-02 is rendered as the report footer on every page of every shortlist report — PDF and Word.",
            "rows": [
                ["PDF report", "Footer on every page via WeasyPrint @page footer rule", "Architect", "Spec'd"],
                ["Word report", "Footer applied to all sections via python-docx", "Architect", "Spec'd"],
                ["Preview screen", "Displayed in full before unlock", "Architect", "Spec'd"],
                ["Historical reports", "Already-generated reports retain their original footer; not retroactively updated", "Architect", "Decision"],
            ],
        },
        "5. Attorney Review Status": {
            "narrative": "D-02 v3.7 wording requires attorney review against Namibian contract law (enforceability of liability exclusions) before going live.",
            "rows": [
                ["Status", "Drafted — pending attorney review", "Legal", "Pending"],
                ["Reviewer", "TBD — qualified Namibian attorney", "Legal", "Pending"],
                ["Target sign-off date", "Before first live shortlist report is generated", "Sponsor", "Required"],
                ["Branding compliance", "Aligned with §31 — 'proprietary matching system' replaces 'artificial intelligence matching engine'", "Marketing", "Confirmed"],
            ],
        },
        "6. Attorney Notes": {
            "narrative": "Attorney review questions captured for the reviewer. Notes from the original v3.6 disclaimer master are carried forward.",
            "rows": [
                ["Liability exclusion clause", "Review enforceability under Namibian contract law; consider proportionate limitation as alternative", "Attorney", "Open"],
                ["Verification disclaimer", "Confirm wording adequately discloses lack of independent verification", "Attorney", "Open"],
                ["Automated processing wording", "Confirm 'proprietary matching system using automated data processing' satisfies any current or forthcoming transparency requirements", "Attorney", "Open"],
                ["Recipient confidentiality clause", "Standard wording — confirm enforceability", "Attorney", "Open"],
                ["Section 31 compatibility", "Confirm wording removal of 'artificial intelligence' is legally sound — automated nature still disclosed", "Attorney", "Open"],
            ],
        },
    },
}
