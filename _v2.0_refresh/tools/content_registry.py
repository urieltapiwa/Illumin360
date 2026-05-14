"""
Illumin360 v2.0 content registry.

Each entry maps a template path (relative to the Illumin360 root) to a content
dict consumed by populator.populate().

Content structure per document:
   {
     "v2_change_description": "...",
     "sections": {
        "<heading text>": {
           "narrative": "...",            # replaces guidance text
           "rows": [["c1","c2","c3","c4"], ...]   # body rows for placeholder table
        },
        ...
     }
   }
"""

REGISTRY = {}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-01-001 — Business Case
# ════════════════════════════════════════════════════════════════════════════
REGISTRY["01_Project_Initiation/Business_Case/ILLM-01-001_Business_Case_v1_0.docx"] = {
    "v2_change_description": (
        "Populated against v3.7 corrected spec. Adds Founder Programme GTM, "
        "Auto-Application Engine as defining feature, three-segment subscription "
        "model (Professional, Student, Business), open-source stack alignment, "
        "AI services cost model."
    ),
    "sections": {
        "2. Executive Summary": {
            "narrative": (
                "Illumin360 is a subscription-based talent matching and recruitment "
                "platform for Namibia, developed by Illumin Investments CC. The "
                "platform serves three customer segments through five portals: "
                "Professionals seeking passive discoverability, Students seeking "
                "graduate-programme scouting, Businesses seeking pre-curated talent, "
                "supported by Administrator and Support staff portals. The defining "
                "feature is the Auto-Application Engine — candidates are surfaced "
                "to relevant requests without applying, eliminating missed "
                "opportunities on either side. Revenue comes from Professional and "
                "Student subscriptions, Business subscription tiers, and per-request "
                "shortlist and candidate unlock fees. The launch strategy is "
                "anchored by the Illumin360 Founder Programme — permanent free "
                "accounts for the first 300 Professionals and permanent Growth-tier "
                "subscriptions for the first 50 Businesses."
            ),
            "rows": [
                ["Platform purpose", "Subscription-based talent matching and recruitment for Namibia", "Sponsor", "Approved"],
                ["Customer segments", "Professionals, Students, Businesses — each subscription-based", "Sponsor", "Approved"],
                ["Defining feature", "Auto-Application Engine — candidates surfaced without applying", "Architect", "Approved"],
                ["Portals", "5 — Professional, Student, Business, Administrator, Support", "Architect", "Approved"],
                ["Launch strategy", "Founder Programme — 300 Professional + 50 Business permanent accounts", "Marketing", "Approved"],
                ["Authority", "v3.7 Illumin360 Complete Technical Specification", "Architect", "Current"],
            ],
        },
        "3. Problem Statement": {
            "narrative": (
                "Recruitment in Namibia is bottlenecked by agency cost (15–25% of "
                "annual salary per hire), agency speed (1–3 weeks per shortlist), "
                "and lack of transparency (employers see only the candidates the "
                "agency chooses to surface). Professionals lack a way to be "
                "passively discoverable without scattering applications across "
                "individual postings. Students approaching graduation lack a "
                "channel to be scouted by employers before they hit the open "
                "market. Businesses lack benchmarking data to understand why they "
                "lose talent to competitors. The market is structurally inefficient "
                "for all three segments."
            ),
            "rows": [
                ["Cost problem", "Agencies charge NAD 30,000–80,000 per professional hire", "—", "Confirmed"],
                ["Speed problem", "Agency shortlist takes 1–3 weeks; candidates lost to competitors", "—", "Confirmed"],
                ["Transparency problem", "Agency selection is opaque; no audit trail", "—", "Confirmed"],
                ["Professional problem", "No passive discoverability channel — must apply individually", "—", "Confirmed"],
                ["Student problem", "No scouting channel ahead of graduation", "—", "Confirmed"],
                ["Business benchmarking problem", "No data on workforce gaps vs industry", "—", "Confirmed"],
            ],
        },
        "4. Proposed Solution": {
            "narrative": (
                "Illumin360 delivers a five-portal platform with the Auto-Application "
                "Engine as the central mechanism. Active Professional and Student "
                "subscriptions act as standing applications against every "
                "Business recruitment request that fits the candidate's stated "
                "profile and consent. The Illumin360 matching engine ranks "
                "candidates and produces a defensible shortlist report in hours. "
                "Businesses pay a flat per-request fee plus optional subscription "
                "tier for predictable monthly value. Professionals and Students "
                "subscribe for ongoing discoverability. Administrators oversee "
                "compliance and operations; Support staff handle tickets, "
                "verification, and dispute resolution. The platform is built on a "
                "fully open-source runtime stack (Keycloak, RabbitMQ, Grafana, "
                "PostgreSQL, MinIO) with third-party AI services for matching."
            ),
            "rows": [
                ["Five portals", "Professional, Student, Business, Administrator, Support", "Architect", "Designed"],
                ["Auto-Application Engine", "Standing application across all matching requests", "Architect", "Designed"],
                ["Matching engine", "Two-pass — hard filters + weighted scoring; shortlist in hours", "Architect", "Designed"],
                ["Shortlist report", "Match scores, candidate analysis, methodology disclosure", "Architect", "Designed"],
                ["Internal recruitment", "Private branded portal for Business internal vacancies", "Architect", "Designed"],
                ["Open-source stack", "Keycloak IAM, RabbitMQ MQ, Grafana observability, PostgreSQL", "Architect", "Confirmed"],
            ],
        },
        "5. Strategic Alignment": {
            "narrative": (
                "The platform aligns with Illumin Investments CC's strategic "
                "objective to deliver subscription-based technology platforms for "
                "the Namibian market. It supports the corporate social "
                "responsibility commitment to youth employment through the free "
                "Student CSR programme. The Auto-Application principle is a "
                "differentiating proposition not currently offered by any local "
                "competitor. The standards orientation (ISO/IEC 27001, ISO 9241, "
                "OpenAPI, DDD, Microservices) supports enterprise readiness for "
                "subsequent regional expansion."
            ),
            "rows": [
                ["Corporate strategy", "Subscription technology platforms for Namibia", "—", "Aligned"],
                ["CSR commitment", "Free Student tier — youth employment focus", "—", "Aligned"],
                ["Market differentiation", "Auto-Application — no comparable local offering", "—", "Confirmed"],
                ["Standards orientation", "ISO 27001, 9241, OpenAPI, DDD — enterprise-ready", "—", "Designed"],
                ["Regional expansion path", "Architecture supports multi-jurisdiction deployment", "—", "Designed"],
            ],
        },
        "6. Cost-Benefit Analysis": {
            "narrative": (
                "Operating costs are dominated by infrastructure (hosting the "
                "open-source stack) and third-party AI services. AI services scale "
                "from NAD 77/month in Year 1 to approximately NAD 1,116/month in "
                "Year 3 — under 0.2% of projected revenue. Revenue scales with "
                "subscription adoption across all three segments and per-request "
                "fees from Businesses. The Founder Programme is a discrete launch "
                "cost (forgone recurring revenue from the first 300 Professionals) "
                "justified by the bootstrap value of a credible talent pool from "
                "day one."
            ),
            "rows": [
                ["Year 1 AI services", "≈ NAD 77/month — Claude + Google Vision", "Finance", "Projected"],
                ["Year 3 AI services", "≈ NAD 1,116/month at projected scale", "Finance", "Projected"],
                ["AI as % of revenue", "0.19% at Year 3 — most efficient operating component", "Finance", "Projected"],
                ["Professional subscription", "NAD 299–1,299 per term — primary recurring revenue", "Finance", "Confirmed"],
                ["Business subscription tiers", "NAD 0 / 1,500 / 3,500 / 10,000 per month", "Finance", "Indicative"],
                ["Per-request fees", "NAD 1,725 standard, NAD 2,300 internal incl. VAT", "Finance", "Confirmed"],
                ["Candidate unlock", "NAD 402.50 incl. VAT", "Finance", "Confirmed"],
                ["Founder Programme cost", "Forgone recurring revenue from first 300 Professionals", "Finance", "Approved"],
                ["Founding Partner cost", "Permanent Growth tier at NAD 0 for 50 Businesses", "Finance", "Approved"],
            ],
        },
        "7. Key Risks and Mitigation": {
            "narrative": (
                "Material risks are grouped into market, technical, compliance, "
                "operational, and commercial categories. The risk register "
                "(ILLM-01-005) maintains the live register; the entries below are "
                "the top-tier risks at business-case level."
            ),
            "rows": [
                ["Talent pool cold start", "Founder Programme — 300 Professional + 50 Business launch quota", "Marketing", "Mitigated"],
                ["Auto-Application consent challenge", "Explicit consent text at registration; opt-outs by industry/employer/type", "Legal", "Designed"],
                ["AI vendor outage", "Local Ollama fallback option; graceful degradation to keyword matching", "Architect", "Designed"],
                ["Branding leak (Section 31)", "Post-generation filter on AI Assistant; quarterly client-facing audit", "Marketing", "Mitigated"],
                ["Photo blind-screening bypass", "Structural — photo not in shortlist projection query", "Architect", "Mitigated"],
                ["Compliance — discriminatory filter use", "D-04 warning, mandatory justification, admin approval", "Legal", "Mitigated"],
                ["PCI-DSS scope creep", "Hosted payment page model — SAQ A only, no card data on platform", "Architect", "Mitigated"],
                ["Founder quota race condition", "SELECT FOR UPDATE serialisation, load-tested", "Architect", "Mitigated"],
            ],
        },
        "8. Recommendation": {
            "narrative": (
                "Authorise the platform for development under the v3.7 corrected "
                "specification. Proceed with the Phase 1 (Core Talent Pool) build "
                "including Auto-Application Engine and Founder Programme quota "
                "enforcement from day one. Subsequent phases follow the 8-phase "
                "plan in spec §27."
            ),
            "rows": [
                ["Authorise", "Phase 1 build per v3.7 spec — Core Talent Pool + Auto-Application + Founder", "Sponsor", "Pending"],
                ["Confirm tech stack", "Keycloak, RabbitMQ, Grafana OSS, PostgreSQL, MinIO (per §36)", "Sponsor", "Confirmed"],
                ["Sign-off Founder pricing", "Permanent free for first 300 Professionals; Growth tier for first 50 Businesses", "Sponsor", "Pending"],
                ["Commercial review", "§37 Business subscription tier prices — Starter/Growth/Enterprise", "Sponsor", "Pending"],
            ],
        },
        "9. Approval": {
            "narrative": (
                "Approval is sought from the Project Sponsor (CEO, Illumin "
                "Investments CC) to proceed with the Phase 1 build per the v3.7 "
                "corrected specification. The sign-off below confirms commitment "
                "to the funding, timeline, and scope described in this Business "
                "Case."
            ),
            "rows": [
                ["Project Sponsor", "Approval to proceed — v3.7 spec, Phase 1 scope", "CEO Illumin Investments CC", "Pending"],
                ["Commercial Review", "§37 subscription tier pricing", "CFO / CEO", "Pending"],
                ["Architecture Review", "v3.7 spec sign-off — Sections 32–37 amendments", "Software Engineer & Architect", "Drafted"],
                ["Funding", "Phase 1 budget allocation", "Sponsor", "Pending"],
            ],
        },
    },
}

# ════════════════════════════════════════════════════════════════════════════
# ILLM-02-002 — Functional Requirements
# ════════════════════════════════════════════════════════════════════════════
REGISTRY["02_Requirements/Functional_Requirements/ILLM-02-002_Functional_Requirements_v1_0.docx"] = {
    "v2_change_description": (
        "Populated from v3.7 spec. FR families FR-1 through FR-39 covering "
        "Professional, Student, Business modules, matching engine, Auto-Application, "
        "Founder, AI Assistant, adaptive weighting, gap analysis, RLHF, assets, "
        "PWA, social features, badges, video, Support Portal, Benchmarking, "
        "Business subscription tiers."
    ),
    "sections": {
        "2. Purpose": {
            "narrative": (
                "This specification enumerates the platform's functional "
                "requirements. Each requirement traces to a section of the v3.7 "
                "spec and is testable. Non-functional requirements are in "
                "ILLM-02-003."
            ),
            "rows": [
                ["Document purpose", "Enumerate functional requirements for Phase 1 through Phase 8", "Architect", "Active"],
                ["Traceability", "Every FR links back to a v3.7 spec section", "Architect", "Active"],
                ["Testability", "Each FR has acceptance criteria in the test plans (folder 08)", "QA", "Active"],
            ],
        },
        "3. Scope": {
            "narrative": (
                "In scope: all functional behaviour of the 5 portals (Professional, "
                "Student, Business, Administrator, Support) and all back-end "
                "services. Out of scope: infrastructure (covered in ILLM-03-001 "
                "Architecture), security details (ILLM-03-007), database schema "
                "(ILLM-03-004)."
            ),
            "rows": [
                ["In scope", "Functional behaviour of all 5 portals and back-end services", "—", "Defined"],
                ["Out of scope — infra", "Covered in Architecture Diagrams (ILLM-03-001)", "—", "Cross-ref"],
                ["Out of scope — security", "Covered in Security Design (ILLM-03-007)", "—", "Cross-ref"],
                ["Out of scope — schema", "Covered in Database Design (ILLM-03-004)", "—", "Cross-ref"],
            ],
        },
        "4. Functional Requirements": {
            "narrative": (
                "Requirements are grouped into 19 families. Each family is "
                "numbered FR-N.x. Detailed text per family is in the rows below "
                "(top-level summary) — full requirement statements are maintained "
                "in the source v3.7 spec sections referenced."
            ),
            "rows": [
                ["FR-1 Registration", "Professional, Student, Business registration via Keycloak SSO", "Spec §3.1, 4.1, 5", "Phase 1"],
                ["FR-2 Profile management", "Profile fields per segment; CV/photo/video upload", "Spec §3, 4, 24", "Phase 1"],
                ["FR-3 Subscription mgmt", "Tiered subscriptions for all 3 segments; renewal reminders", "Spec §2, §37", "Phase 1–2"],
                ["FR-4 Recruitment request", "Business creates request with 4 search modes + auto-application", "Spec §5, §7, §33", "Phase 1"],
                ["FR-5 Matching engine", "Two-pass — hard filters + weighted scoring; 30s for 5k candidates", "Spec §8", "Phase 1"],
                ["FR-6 Shortlist & report", "PDF + Word with methodology disclosure", "Spec §9, §10", "Phase 2"],
                ["FR-7 Payment", "Hosted gateway, webhook-driven, PCI-DSS SAQ A", "Spec §11", "Phase 2"],
                ["FR-8 Notification", "Email + in-product; 52+ triggers per segment", "Spec §12", "Phase 2"],
                ["FR-9 Compliance controls", "Sensitive filter warning D-04; declaration D-03; audit logs", "Spec §15", "Phase 3"],
                ["FR-10 Admin portal", "Full system access; reports and analytics; refund processing", "Spec §16", "Phase 3"],
                ["FR-11 Internal recruitment", "Private branded portal; auto-close; consolidated billing", "Spec §6", "Phase 4"],
                ["FR-12 Student lifecycle", "Verification (3 methods); graduation upgrade flow", "Spec §4", "Phase 5"],
                ["FR-13 Social features", "Profile card, demand feed, insights, spotlight, referral, talent report", "Spec §21", "Phase 5–6"],
                ["FR-14 Adaptive weighting", "Custom weights per request; sum-to-100; immutable audit", "Spec §22.1", "Phase 6"],
                ["FR-15 Gap analysis", "70–85% band sub-blocks; structural traceability", "Spec §22.2", "Phase 6"],
                ["FR-16 RLHF data", "Day-14 employer feedback; 500-record threshold for Phase 8", "Spec §22.3", "Phase 6"],
                ["FR-17 Asset mgmt", "Logos (business/institution); blind-screening photo", "Spec §24", "Phase 6"],
                ["FR-18 PWA", "Manifest, service worker, offline fallback, install prompts", "Spec §25", "Phase 6"],
                ["FR-19 Badges", "11 candidate + 5 employer types; auto-award triggers", "Spec §26", "Phase 5–6"],
                ["FR-20 AI Assistant", "5 instances; per-user-type context; escalation flow", "Spec §29", "Phase 6"],
                ["FR-21 Founder Programme", "Quota enforcement; race-condition serialisation; permanent badges", "Spec §30", "Phase 1"],
                ["FR-22 Branding policy", "No client-facing AI/vendor references; post-gen filter on assistant", "Spec §31", "All phases"],
                ["FR-23 Support Portal", "Tickets, KB, identity verification, abuse moderation", "Spec §32", "Phase 5"],
                ["FR-24 Auto-Application", "Standing-application principle; opt-outs; notification tiers", "Spec §33", "Phase 1"],
                ["FR-25 Benchmarking", "Business workforce vs industry; Professional/Student rank widgets", "Spec §34", "Phase 5–6"],
                ["FR-26 Architecture", "Microservices aligned to DDD; Clean layering; OpenAPI contracts", "Spec §35", "Phase 1+"],
                ["FR-27 OSS stack", "Keycloak, RabbitMQ, Grafana stack, PostgreSQL, MinIO", "Spec §36", "Phase 1+"],
                ["FR-28 Business subscription", "4-tier model + per-request discounts and allowances", "Spec §37", "Phase 2"],
                ["FR-29 Video integration", "60s pitch; transcription at 30% weight; admin moderation", "Spec §23", "Phase 7"],
            ],
        },
        "5. Module Breakdown": {
            "narrative": (
                "Functional requirements are owned by the following bounded-context "
                "services. Each service has its FR families grouped here for "
                "implementation planning."
            ),
            "rows": [
                ["identity-svc", "Keycloak integration; profile shell — FR-1, FR-2", "Architect", "Defined"],
                ["professional-svc", "Professional/Student profile, CV, badges — FR-2, FR-12, FR-19", "Architect", "Defined"],
                ["business-svc", "Business profile, internal portal — FR-2, FR-11", "Architect", "Defined"],
                ["recruitment-svc", "Recruitment requests, compliance — FR-4, FR-9", "Architect", "Defined"],
                ["matching-svc", "Matching engine, auto-application, adaptive weighting, gap analysis — FR-5, FR-14, FR-15, FR-24", "Architect", "Defined"],
                ["reporting-svc", "PDF/Word report generation — FR-6", "Architect", "Defined"],
                ["payment-svc", "Payment + invoicing + subscriptions — FR-3, FR-7, FR-28", "Architect", "Defined"],
                ["notification-svc", "Email + in-product — FR-8", "Architect", "Defined"],
                ["ai-assistant-svc", "Platform Assistant — FR-20", "Architect", "Defined"],
                ["engagement-svc", "Badges, social features, referrals — FR-13, FR-19", "Architect", "Defined"],
                ["benchmarking-svc", "Workforce snapshots, rankings — FR-25", "Architect", "Defined"],
                ["support-svc", "Tickets, KB — FR-23", "Architect", "Defined"],
                ["admin-svc", "Admin dashboard, audit — FR-10", "Architect", "Defined"],
            ],
        },
        "6. Acceptance Criteria": {
            "narrative": (
                "Each FR has acceptance criteria captured in the relevant Test "
                "Cases document under folder 08. Top-level acceptance gates per "
                "phase are summarised here."
            ),
            "rows": [
                ["Phase 1 gate", "All Phase 1 FRs pass; Founder quota race tested; Auto-Application audit ledger working", "QA Lead", "Pending"],
                ["Phase 2 gate", "Reporting, payments, notifications operational; consolidated invoicing live", "QA Lead", "Pending"],
                ["Phase 3 gate", "Privacy, compliance, candidate unlock, admin analytics live", "QA Lead", "Pending"],
                ["Phase 4 gate", "Internal recruitment portal end-to-end; HR notifications working", "QA Lead", "Pending"],
                ["Phase 5 gate", "Student CSR + social features (F1–F5, F7) + Support Portal live", "QA Lead", "Pending"],
                ["Phase 6 gate", "AI Assistant + adaptive weighting + gap analysis + assets + PWA + badges live", "QA Lead", "Pending"],
                ["Phase 7 gate", "Video integration live; transcription cap at 30% verified", "QA Lead", "Pending"],
                ["Phase 8 gate", "RLHF model refinement post-500-record threshold; marketplace expansion", "QA Lead", "Pending"],
            ],
        },
        "7. Traceability to Business Requirements": {
            "narrative": (
                "Full traceability is maintained in the Requirements Traceability "
                "Matrix (ILLM-02-006). Top-level mapping shown here."
            ),
            "rows": [
                ["BR — talent pool access", "FR-2, FR-4, FR-5, FR-24 (Auto-Application)", "Architect", "Traced"],
                ["BR — cost vs agencies", "FR-7, FR-28 (per-request fees, subscription tiers)", "Architect", "Traced"],
                ["BR — speed of shortlist", "FR-5 (30s/5k cands), FR-6 (60s report gen)", "Architect", "Traced"],
                ["BR — transparency", "FR-6 (methodology disclosure), FR-14 (weights_used audit)", "Architect", "Traced"],
                ["BR — internal recruitment", "FR-11 (consolidated billing)", "Architect", "Traced"],
                ["BR — graduate pipeline", "FR-12 (student lifecycle), FR-24 (auto-application)", "Architect", "Traced"],
                ["BR — compliance", "FR-9 (sensitive filter), FR-22 (branding policy)", "Architect", "Traced"],
                ["BR — benchmarking", "FR-25 (Business + Professional + Student benchmarking)", "Architect", "Traced"],
            ],
        },
        "8. Approval": {
            "narrative": (
                "Approval of this functional requirements specification authorises "
                "the design and development teams to proceed against the FR set "
                "above. Changes after approval flow through Change Control "
                "(ILLM-13-005)."
            ),
            "rows": [
                ["Technical Lead", "FR set is complete, testable, and traced", "Software Engineer & Architect", "Pending"],
                ["Project Manager", "FR set aligns to project plan and phase boundaries", "PM", "Pending"],
                ["QA Lead", "FR set is testable; test plans derived from it", "QA Lead", "Pending"],
                ["Sponsor", "FR set delivers the business case", "Sponsor", "Pending"],
            ],
        },
    },
}
