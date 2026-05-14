"""Fixed content for templates with different section names than originally assumed."""

FIXES = {}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-02-001 — Business Requirements (corrected headings)
# ════════════════════════════════════════════════════════════════════════════
FIXES["02_Requirements/Business_Requirements/ILLM-02-001_Business_Requirements_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec. Captures business goals, stakeholder needs, prioritised requirements, constraints, and traceability for all three customer segments and five portals.",
    "sections": {
        "2. Executive Summary": {
            "narrative": "Illumin360 is required to deliver three primary business outcomes: recurring-revenue subscription business serving Professionals, Students, and Businesses; a defensible recruitment process replacing the agency model for mid-tier roles in Namibia; and a scalable open-source-based architecture supporting regional expansion. The Auto-Application Engine is the platform's defining commercial mechanism — candidates surface to relevant requests without applying. The Founder Programme is the launch-stage acquisition mechanism — first 300 Professional and first 50 Business registrations receive permanent benefits.",
            "rows": [
                ["BG-1 Subscription revenue", "Recurring revenue from Professional, Student, Business subscriptions", "Sponsor", "Approved"],
                ["BG-2 Replace agency cost", "NAD 1,725 flat per shortlist vs 15–25% agency fees", "Sponsor", "Approved"],
                ["BG-3 Defensible shortlist", "Methodology disclosure and audit trail in every report", "Architect", "Approved"],
                ["BG-4 Auto-Application", "Candidates surfaced without applying — no missed opportunities", "Architect", "Approved"],
                ["BG-5 Student CSR pipeline", "Free student tier for graduate-employer pipeline", "Sponsor", "Approved"],
                ["BG-6 Regional expansion path", "Architecture and standards support multi-country deployment", "Architect", "Approved"],
            ],
        },
        "3. Background": {
            "narrative": "Recruitment in Namibia is bottlenecked by agency cost, agency speed, and lack of transparency. Professionals lack passive discoverability; Students lack scouting channels ahead of graduation; Businesses lack benchmarking data. The platform addresses all three sides of this market inefficiency through subscription access and the Auto-Application Engine.",
            "rows": [
                ["Cost bottleneck", "Agencies charge NAD 30,000–80,000 per professional hire", "—", "Confirmed"],
                ["Speed bottleneck", "Agency shortlist takes 1–3 weeks per request", "—", "Confirmed"],
                ["Transparency bottleneck", "Agency selection is opaque; no audit trail", "—", "Confirmed"],
                ["Professional gap", "No passive discoverability channel — must apply individually", "—", "Confirmed"],
                ["Student gap", "No scouting channel ahead of graduation", "—", "Confirmed"],
                ["Business gap", "No data on workforce vs industry benchmarks", "—", "Confirmed"],
            ],
        },
        "4. Business Requirements": {
            "narrative": "Business requirements grouped by customer segment plus cross-cutting platform-wide requirements. Each is traced to functional requirements in ILLM-02-002.",
            "rows": [
                ["BR-P-1 Professional discoverability", "Active profile is a standing application to all matching requests", "FR-2, FR-24", "Captured"],
                ["BR-P-2 Professional benchmarking", "Rank dashboard against peers in role category", "FR-25", "Captured"],
                ["BR-P-3 Professional gamification", "Skill quests, certifications, badges, referral programme", "FR-19, FR-25", "Captured"],
                ["BR-S-1 Student CSR free tier", "Free subscription throughout study + 60-day grace period", "FR-12", "Captured"],
                ["BR-S-2 Student scouting", "Pipeline searches and Graduate Trainee Programme mode", "FR-12, FR-24", "Captured"],
                ["BR-S-3 Student leaderboard", "Institution and national ranking by programme", "FR-25", "Captured"],
                ["BR-B-1 Business shortlist", "Flat-fee per-shortlist (NAD 1,725) vs agency model", "FR-4, FR-6, FR-7", "Captured"],
                ["BR-B-2 Business benchmarking", "Workforce vs industry; skill gap analysis", "FR-25", "Captured"],
                ["BR-B-3 Business internal recruitment", "Private branded portal for internal vacancies", "FR-11", "Captured"],
                ["BR-B-4 Business subscription tiers", "Free / Starter / Growth / Enterprise with allowances", "FR-3, FR-28", "Captured"],
                ["BR-A-1 Admin oversight", "Full system access, compliance reviews, audit", "FR-10", "Captured"],
                ["BR-SU-1 Support workspace", "Ticket triage, KB, identity verification, abuse moderation", "FR-23", "Captured"],
                ["BR-X-1 Defining feature", "Auto-Application Engine — across all segments and requests", "FR-24", "Captured"],
                ["BR-X-2 Launch strategy", "Founder Programme — 300 + 50 permanent accounts", "FR-21", "Captured"],
            ],
        },
        "5. Requirement Prioritisation": {
            "narrative": "MoSCoW prioritisation aligned to the 8-phase plan in spec §27.",
            "rows": [
                ["Must — Phase 1", "Core registration, profile, matching engine, Auto-Application, Founder grants, compliance", "Architect", "Mandatory"],
                ["Must — Phase 2", "Reporting, payments, notifications, subscription tiers (Business + Professional + Student)", "Architect", "Mandatory"],
                ["Must — Phase 3", "Privacy, compliance audits, candidate unlock, admin analytics", "Architect", "Mandatory"],
                ["Should — Phase 4", "Internal recruitment link with consolidated billing", "Architect", "Strong"],
                ["Should — Phase 5", "Student CSR, social features F1–F5/F7, Support Portal, leaderboards", "Architect", "Strong"],
                ["Should — Phase 6", "AI Assistant, adaptive weighting, gap analysis, RLHF capture, assets (blind-screening), PWA, badges", "Architect", "Strong"],
                ["Could — Phase 7", "Video integration (candidate elevator pitch)", "Architect", "Premium"],
                ["Could — Phase 8", "RLHF model refinement, marketplace expansion", "Architect", "Future"],
                ["Won't (initial)", "Mobile native apps (PWA covers); recruiter chat/messaging; ATS integration", "Architect", "Out of scope"],
            ],
        },
        "6. Assumptions and Constraints": {
            "narrative": "Assumptions are subject to validation; constraints are non-negotiable.",
            "rows": [
                ["Assumption — demand", "Sufficient Professional and Business demand in Namibia to reach Founder quotas", "Sponsor", "To validate"],
                ["Assumption — university partnerships", "UNAM, NUST, IUM agreeable to institutional verification programme", "Marketing", "To validate"],
                ["Assumption — payment gateway", "Local Namibian gateway with hosted-page integration available", "Architect", "To validate"],
                ["Assumption — AI vendor stability", "Anthropic Claude Sonnet 4.6 available with stable pricing", "Architect", "Assumed"],
                ["Constraint — jurisdiction", "Namibia — Labour Act 11 of 2007, Electronic Transactions Act 4 of 2019, Article 10", "Legal", "Mandatory"],
                ["Constraint — PCI-DSS", "SAQ A only — no card data stored on platform", "Architect", "Mandatory"],
                ["Constraint — branding policy", "§31 — no client-facing AI/vendor references", "Marketing", "Mandatory"],
                ["Constraint — blind screening", "Photos not in matching engine or shortlist projection", "Architect", "Mandatory"],
                ["Constraint — standards", "ISO/IEC 12207, 81346, 9241, 27001; OpenAPI; DDD/Clean/Microservices/REST/.NET", "Architect", "Required"],
                ["Constraint — open-source preference", "Runtime stack OSS where possible; AI services external", "Architect", "Preferred"],
            ],
        },
        "7. Traceability": {
            "narrative": "Top-level mapping from business requirements to functional requirements. Full matrix in Requirements Traceability Matrix (ILLM-02-006).",
            "rows": [
                ["BR-P-1 → FR-2, FR-24", "Profile + Auto-Application", "Architect", "Traced"],
                ["BR-P-2/P-3 → FR-19, FR-25", "Gamification, badges, benchmarking", "Architect", "Traced"],
                ["BR-S-1/S-2/S-3 → FR-12, FR-24, FR-25", "Student lifecycle, scouting, leaderboard", "Architect", "Traced"],
                ["BR-B-1 → FR-4, FR-6, FR-7", "Recruitment request + report + payment", "Architect", "Traced"],
                ["BR-B-2 → FR-25", "Workforce benchmarking", "Architect", "Traced"],
                ["BR-B-3 → FR-11", "Internal recruitment portal", "Architect", "Traced"],
                ["BR-B-4 → FR-3, FR-28", "Subscription tiers + per-request allowances", "Architect", "Traced"],
                ["BR-A-1 → FR-10", "Admin oversight", "Architect", "Traced"],
                ["BR-SU-1 → FR-23", "Support Portal", "Architect", "Traced"],
                ["BR-X-1 → FR-24", "Auto-Application Engine", "Architect", "Traced"],
                ["BR-X-2 → FR-21", "Founder Programme", "Architect", "Traced"],
            ],
        },
        "8. Approval": {
            "narrative": "Approval of this business requirements specification authorises the design and development teams to proceed.",
            "rows": [
                ["Project Sponsor", "Business goals and prioritisation approved", "CEO Illumin Investments CC", "Pending"],
                ["Marketing Lead", "Segment framing and value propositions approved", "Marketing", "Pending"],
                ["Architect", "Constraints feasible; standards alignment confirmed", "Software Engineer & Architect", "Drafted"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-02-004 — Use Cases / User Stories (corrected headings)
# ════════════════════════════════════════════════════════════════════════════
FIXES["02_Requirements/Use_Cases_User_Stories/ILLM-02-004_Use_Cases_User_Stories_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec. Backlog + use case descriptions across all five portals plus cross-cutting flows.",
    "sections": {
        "2. Purpose": {
            "narrative": "This document captures use cases and user stories for all five Illumin360 portals (Professional, Student, Business, Administrator, Support) plus cross-cutting flows including Auto-Application, Founder grant, internal portal close-and-process, and AI Assistant escalation. Use cases trace to functional requirements (ILLM-02-002) and to test cases (folder 08).",
            "rows": [
                ["Purpose", "Capture actor goals as use cases and stories for design and testing", "Architect", "Active"],
                ["Traceability", "Each UC links to FRs and to test cases", "Architect", "Active"],
            ],
        },
        "3. Scope": {
            "narrative": "Scope covers all functional flows for the 5 portals and the back-end services that support them. Out of scope: infrastructure, schema, and security details (held in their own documents).",
            "rows": [
                ["In scope", "All flows for Professional, Student, Business, Administrator, Support portals", "—", "Defined"],
                ["Out — infra", "Architecture Diagrams (ILLM-03-001)", "—", "Cross-ref"],
                ["Out — schema", "Database Design (ILLM-03-004)", "—", "Cross-ref"],
                ["Out — security details", "Security Design (ILLM-03-007)", "—", "Cross-ref"],
            ],
        },
        "4. User Story Backlog": {
            "narrative": "User stories in As-a / I-want / So-that form. Grouped by actor and prioritised against the 8-phase plan.",
            "rows": [
                ["US-P-01", "As a Professional, I want to register and verify my email so that I can be discoverable", "FR-1, Phase 1", "Ready"],
                ["US-P-02", "As a Professional, I want my profile to be a standing application so that I don't have to apply to each posting", "FR-24, Phase 1", "Ready"],
                ["US-P-03", "As a Professional, I want to opt out of specific industries or employers so that I retain control over my visibility", "FR-24, Phase 1", "Ready"],
                ["US-P-04", "As a Professional, I want to see my rank against peers so that I know where I stand", "FR-25, Phase 5", "Ready"],
                ["US-P-05", "As a Professional, I want skill suggestions so that I can improve my match position", "FR-25, Phase 6", "Ready"],
                ["US-P-06", "As a Professional, I want a referral link so that I earn free months from successful referrals", "FR-13, Phase 5", "Ready"],
                ["US-S-01", "As a Student, I want to register free and verify my enrolment so that I can be in the pipeline pool", "FR-12, Phase 5", "Ready"],
                ["US-S-02", "As a Student, I want to be ranked among peers at my institution so that I can target graduate programmes", "FR-25, Phase 5", "Ready"],
                ["US-S-03", "As a Student, I want a 60-day grace period after graduation so that I can transition smoothly", "FR-12, Phase 5", "Ready"],
                ["US-B-01", "As a Business, I want to create a recruitment request with custom filters so that I can find matching candidates", "FR-4, Phase 1", "Ready"],
                ["US-B-02", "As a Business, I want shortlist scores with methodology disclosure so that my hiring is defensible", "FR-6, Phase 2", "Ready"],
                ["US-B-03", "As a Business, I want to set custom weights so that the matching reflects role priorities", "FR-14, Phase 6", "Ready"],
                ["US-B-04", "As a Business, I want an internal recruitment portal so that staff apply through a private branded link", "FR-11, Phase 4", "Ready"],
                ["US-B-05", "As a Business, I want a workforce benchmarking report so that I understand gaps vs industry", "FR-25, Phase 6", "Ready"],
                ["US-B-06", "As a Founding Partner, I want my Growth tier permanently free so that my early commitment is recognised", "FR-21, FR-28, Phase 1", "Ready"],
                ["US-A-01", "As an Administrator, I want to review compliance justifications so that I can approve or reject sensitive-filter requests", "FR-9, Phase 3", "Ready"],
                ["US-A-02", "As an Administrator, I want to process refunds so that disputed payments can be reversed", "FR-10, Phase 3", "Ready"],
                ["US-SU-01", "As Support staff, I want a ticket workspace so that I can triage and resolve customer issues", "FR-23, Phase 5", "Ready"],
                ["US-SU-02", "As Support staff, I want to author KB articles so that the AI Assistant has fresh content", "FR-23, Phase 5", "Ready"],
            ],
        },
        "5. Use Case Descriptions": {
            "narrative": "Use cases described at flow level. Pre-conditions, main flow, exceptions, and post-conditions captured.",
            "rows": [
                ["UC-X-01 Auto-Application", "Pre: Professional/Student has active profile and matching criteria. Flow: Business creates request → engine runs → auto_application_matches written per candidate considered → shortlist surfaced to Business. Post: candidate notified per their tier preference.", "Phase 1", "Designed"],
                ["UC-X-02 Founder grant (atomic)", "Pre: New user registration in progress. Flow: SELECT FOR UPDATE on founder count → if under quota, INSERT founder_registrations + UPDATE is_founder + INSERT badge + INSERT subscription (Professional only) atomically. Post: badge appears on profile after first login.", "Phase 1", "Designed"],
                ["UC-X-03 Internal portal close-and-process", "Pre: Business has open internal portal with closing time set. Flow: cron at closing time → lock portal → enqueue matching job → matching runs → report generated → Business notified. Post: portal status = closed; report ready for unlock.", "Phase 4", "Designed"],
                ["UC-X-04 AI Assistant escalation", "Pre: User in active assistant conversation. Flow: user clicks Connect me with the Illumin team → conversation transcript + context snapshot captured → support ticket created → email sent to support inbox. Post: ticket in support queue with full context.", "Phase 6", "Designed"],
                ["UC-X-05 Graduate upgrade", "Pre: Student at end of grace period. Flow: select paid plan → pay via gateway → graduate_student_to_job_seeker() runs atomically → profile_type changes; subscription transitions. Post: profile retained; subscription billing live.", "Phase 5", "Designed"],
                ["UC-P-01 Register Professional", "Pre: Email not yet registered. Flow: Keycloak SSO sign-up → consent D-05 + Auto-Application clause → trigger Founder check → profile shell created. Post: account active; verification email sent.", "Phase 1", "Designed"],
                ["UC-B-03 Create recruitment request", "Pre: Business authenticated. Flow: choose search mode → set filters (sensitive triggers D-04) → optionally set custom weights → confirm D-03 declaration → submit. Post: request locked; matching engine queued.", "Phase 1, 6 (weights)", "Designed"],
                ["UC-B-06 Unlock shortlist report", "Pre: Anonymous preview seen. Flow: click unlock → gateway checkout → webhook confirms → reports.unlocked_at set → PDF + Word generated. Post: report available for download (signed 48h URL).", "Phase 2", "Designed"],
            ],
        },
        "6. Acceptance Criteria": {
            "narrative": "Acceptance criteria per use case are captured in the test cases folder (08). Top-level gates summarised here.",
            "rows": [
                ["UC-X-01", "Every request creates one auto_application_matches row per considered candidate; opt-outs honoured", "QA", "Pending"],
                ["UC-X-02", "Race condition test: 50 concurrent registrations all attempting slot 300 — exactly one wins", "QA", "Pending"],
                ["UC-X-03", "Portal locks at exact closing time; report generated within 60 seconds of lock", "QA", "Pending"],
                ["UC-X-04", "Escalated ticket arrives in support queue with full transcript and context snapshot", "QA", "Pending"],
                ["UC-B-03", "Submission blocked until D-03 ticked; weights validated to sum=100 if custom", "QA", "Pending"],
                ["UC-B-06", "Webhook-driven unlock — browser redirect never used as confirmation; signed URL expires correctly", "QA", "Pending"],
            ],
        },
        "7. Traceability to Business Requirements": {
            "narrative": "Use case to business requirement mapping (high-level).",
            "rows": [
                ["UC-X-01 Auto-Application → BR-X-1, BR-P-1, BR-S-2", "Defining-feature trace", "Architect", "Traced"],
                ["UC-X-02 Founder grant → BR-X-2", "Launch strategy trace", "Architect", "Traced"],
                ["UC-B-03 Request creation → BR-B-1", "Business shortlist trace", "Architect", "Traced"],
                ["UC-B-05 Benchmarking → BR-B-2", "Workforce benchmarking trace", "Architect", "Traced"],
                ["UC-B-04 Internal portal → BR-B-3", "Internal recruitment trace", "Architect", "Traced"],
                ["UC-S-01 Student lifecycle → BR-S-1, BR-S-2", "Student CSR trace", "Architect", "Traced"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-03-002 — Technology Stack (corrected, condensed headings)
# ════════════════════════════════════════════════════════════════════════════
FIXES["03_System_Design/High_Level_Design/Technology_Stack/ILLM-03-002_Technology_Stack_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §36. Open-source-first runtime with Keycloak, RabbitMQ, Grafana OSS confirmed. .NET implied as application language pending confirmation.",
    "sections": {
        "2. Technology Stack Overview": {
            "narrative": "The Illumin360 platform runs on a fully open-source runtime stack. The only non-OSS dependencies at runtime are the third-party AI services (Anthropic Claude Sonnet 4.6, Google Cloud Vision OCR) and the chosen payment gateway. Application code is in C# / ASP.NET Core (implied by §35 Microsoft .NET Naming Guidelines and pending explicit confirmation). Architecture is microservices aligned to DDD bounded contexts with Clean Architecture layering.",
            "rows": [
                ["Runtime basis", "Open-source first; AI services and payment gateway are the only non-OSS dependencies", "Architect", "Confirmed"],
                ["Application language", ".NET (C# / ASP.NET Core) — implied; pending explicit confirmation", "Architect", "Pending"],
                ["Architecture style", "Microservices aligned to DDD bounded contexts; Clean Architecture per service", "Architect", "Confirmed"],
                ["Inter-service comms", "Async via RabbitMQ; sync via REST through API gateway (Kong/Traefik)", "Architect", "Confirmed"],
                ["Standards", "ISO/IEC 12207, 81346, 9241, 27001; OpenAPI 3.x; REST; .NET naming", "Architect", "Required"],
            ],
        },
        "3. Technology Evaluation": {
            "narrative": "Each major choice was evaluated against alternatives. Keycloak preferred over Auth0 (cost, sovereignty); RabbitMQ over Redis-as-queue (durability, routing flexibility); PostgreSQL over MySQL (pgvector, JSONB, partitioning); MinIO over self-built file storage (S3 compatibility, ops maturity); Grafana stack over commercial APM (cost, openness, integration). Detailed evaluation rationale held in ADRs (ILLM-13-008 Decision Log).",
            "rows": [
                ["IAM choice", "Keycloak chosen — cost, sovereignty, OIDC standard, federation support", "Architect", "Decided"],
                ["Message queue", "RabbitMQ chosen — durability, routing flexibility, mature ops", "Architect", "Decided"],
                ["Database", "PostgreSQL 15+ chosen — pgvector, JSONB, partitioning, mature ecosystem", "Architect", "Decided"],
                ["Object storage", "MinIO chosen — S3 API compatibility, on-prem option, free egress", "Architect", "Decided"],
                ["Observability", "Grafana LGTM stack chosen — cost, openness, integration with k6", "Architect", "Decided"],
                ["Secrets", "HashiCorp Vault OSS chosen — industry standard, K8s integration", "Architect", "Decided"],
                ["Workflows", "Temporal OSS chosen — removes bespoke saga code for long workflows", "Architect", "Decided"],
                ["Container orchestration", "Kubernetes or K3s — selection based on operational team capacity", "Architect", "Open"],
            ],
        },
        "4. Selected Technologies by Layer": {
            "narrative": "Layered view of the confirmed and recommended technology choices. See spec §36 for the comprehensive list.",
            "rows": [
                ["Identity & Access", "Keycloak (OIDC/OAuth2 authority, MFA, federation)", "Architect", "Confirmed"],
                ["Application runtime", ".NET 8+ / ASP.NET Core; EF Core or Dapper; Polly; Serilog", "Architect", "Pending confirm"],
                ["Messaging", "RabbitMQ (async events); Temporal (long-running workflows)", "Architect", "Confirmed"],
                ["Data store — relational", "PostgreSQL 15+", "Architect", "Confirmed"],
                ["Data store — vector", "pgvector extension (PostgreSQL)", "Architect", "Confirmed"],
                ["Data store — cache/session", "Redis", "Architect", "Confirmed"],
                ["Data store — object", "MinIO (S3-compatible)", "Architect", "Confirmed"],
                ["Observability", "Grafana OSS + Prometheus + Loki + Tempo + OpenTelemetry", "Architect", "Confirmed"],
                ["Edge & gateway", "Kong/Traefik API gateway; NGINX reverse proxy", "Architect", "Recommended"],
                ["Security tooling", "ClamAV virus scan; HashiCorp Vault secrets; Trivy container scan; OWASP ZAP DAST", "Architect", "Recommended"],
                ["Document processing", "Apache Tika; Tesseract OCR fallback; WeasyPrint PDF; python-docx + docxtpl", "Architect", "Confirmed"],
                ["Container & orchestration", "Docker; Kubernetes or K3s; Helm; Argo CD", "Architect", "Recommended"],
                ["AI services (third-party)", "Anthropic Claude Sonnet 4.6 (claude-sonnet-4-6); Google Cloud Vision OCR", "Architect", "Confirmed"],
                ["Frontend (initial)", "TailwindCSS utility classes per existing component library; framework TBD", "Architect", "Indicative"],
                ["Testing", "k6 load testing; Playwright E2E; Hoppscotch API testing", "QA", "Recommended"],
                ["Marketing automation", "Mautic (referral funnels, talent report gating)", "Marketing", "Recommended"],
            ],
        },
        "5. Licence and Cost Summary": {
            "narrative": "All runtime stack components are open-source under permissive or copyleft licences compatible with commercial use. Hosting costs depend on Kubernetes capacity. AI services scale per usage per spec §28. No software licence fees apply to the open-source components.",
            "rows": [
                ["Keycloak", "Apache 2.0", "No licence fee", "Confirmed"],
                ["RabbitMQ", "Mozilla Public Licence 2.0", "No licence fee", "Confirmed"],
                ["Grafana OSS, Prometheus, Loki, Tempo, OpenTelemetry, k6", "AGPL-3.0 / Apache 2.0", "No licence fee", "Confirmed"],
                ["PostgreSQL", "PostgreSQL Licence (BSD-style)", "No licence fee", "Confirmed"],
                ["MinIO", "AGPL-3.0", "No licence fee for SaaS-style deployment; consider commercial licence if SaaS-with-modifications", "Note"],
                ["Redis", "BSD-3-Clause (RSAL changes — confirm version)", "No licence fee for current versions used", "Verify"],
                ["Kubernetes, Docker, Helm", "Apache 2.0", "No licence fee", "Confirmed"],
                ["HashiCorp Vault OSS", "BUSL — commercial use permitted with restrictions", "Verify acceptable for use case", "Verify"],
                ["Temporal", "MIT", "No licence fee", "Confirmed"],
                ["ClamAV", "GPL v2", "No licence fee", "Confirmed"],
                ["Anthropic API (Claude)", "Pay-per-use", "Year 1 ≈ USD 4.25/month combined", "Confirmed"],
                ["Google Cloud Vision", "Pay-per-use", "Free tier covers initial scale", "Confirmed"],
            ],
        },
        "6. Upgrade and Retirement Strategy": {
            "narrative": "Upgrade cadence and retirement policy. Major version upgrades occur in dedicated planning windows; security patches apply per ILLM-11-002 Monitoring and Alerting policy.",
            "rows": [
                ["Keycloak", "Track LTS releases; upgrade every 12–18 months", "DevOps", "Policy"],
                ["PostgreSQL", "Stay on supported major version; upgrade every 2–3 years", "DevOps", "Policy"],
                ["RabbitMQ", "Track minor LTS; upgrade annually", "DevOps", "Policy"],
                ["Kubernetes", "Track stable minor; upgrade every 6 months", "DevOps", "Policy"],
                ["AI model versions", "Pin Claude model string to claude-sonnet-4-6; evaluate model upgrades quarterly", "Architect", "Policy"],
                ["Retirement — Tesseract OCR fallback", "Retire if Cloud Vision SLA proves sufficient over Year 1", "Architect", "Open"],
                ["Retirement — Ollama self-hosted fallback", "Phase 8 evaluation only", "Architect", "Open"],
                ["Security patches", "Critical: 7 days; High: 30 days; Medium: next planning cycle", "Security", "Policy"],
                ["End-of-life policy", "Components reaching upstream EoL replaced before EoL date", "DevOps", "Policy"],
            ],
        },
    },
}
