"""Batch 3 — Initiation completion (01), Requirements completion (02), all Disclaimers and Compliance Pack (09), Governance (13), Configuration (14)."""

BATCH3 = {}

# ───────────────── Folder 01 — Initiation completion ─────────────────────────

BATCH3["01_Project_Initiation/Feasibility_Study/ILLM-01-002_Feasibility_Study_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec. Multi-dimensional feasibility assessment for the Illumin360 platform build.",
    "sections": {
        "2. Executive Summary": {
            "narrative": "The Illumin360 platform is feasible across all four dimensions assessed: technical (proven open-source stack and well-defined AI integrations), financial (cost model favourable, AI services <0.2% of revenue), operational (single-architect build feasible for Phase 1; operations scale with subscription growth), and legal (Namibian framework supports the platform pending attorney review of disclaimer pack). Schedule feasibility supports a Phase 1 launch within 8–10 weeks of authorisation.",
            "rows": [
                ["Technical", "Feasible — proven OSS stack + Anthropic + Google Vision", "Architect", "Confirmed"],
                ["Financial", "Feasible — AI services <0.2% revenue at Year 3", "Finance", "Confirmed"],
                ["Operational", "Feasible — Phase 1 deliverable by sole architect", "Operations", "Confirmed"],
                ["Schedule", "Feasible — 8–10 weeks Phase 1 per spec §27", "PM", "Confirmed"],
                ["Legal", "Feasible — pending attorney sign-off on disclaimer pack", "Legal", "Conditional"],
            ],
        },
        "3. Technical Feasibility": {
            "narrative": "The architecture uses mature open-source components (PostgreSQL, Keycloak, RabbitMQ, Grafana stack, MinIO) and third-party AI services (Anthropic Claude Sonnet 4.6 and Google Cloud Vision) with documented integration patterns. The 70-migration master schema is already drafted. No exotic or unproven technology choices.",
            "rows": [
                ["Stack maturity", "All OSS components are production-grade with established communities", "Architect", "Confirmed"],
                ["AI vendor stability", "Anthropic Claude Sonnet 4.6 pinned model string; fallback paths defined", "Architect", "Confirmed"],
                ["Schema readiness", "70 migrations drafted covering v1.0–v3.6; v3.7 additions planned", "DBA", "Ready"],
                ["Architecture", "Microservices aligned to DDD bounded contexts — clear service boundaries", "Architect", "Designed"],
                ["Performance targets", "30s matching for 5k pool achievable with PostgreSQL + appropriate indexes", "Architect", "Validated by design"],
                ["Standards alignment", "ISO/IEC 27001, ISO 9241, OpenAPI, DDD, Clean, Microservices, REST, .NET", "Architect", "Designed"],
            ],
        },
        "4. Financial Feasibility": {
            "narrative": "Operating cost dominated by infrastructure (hosting OSS stack on Kubernetes or K3s) and AI services. AI cost is exceptionally efficient — under 0.2% of projected revenue at Year 3. Revenue across three customer segments plus per-request fees supports profitable operation at the Year 1 Founder-launch scale.",
            "rows": [
                ["Year 1 AI cost", "≈ NAD 77/month combined", "Finance", "Projected"],
                ["Year 3 AI cost", "≈ NAD 1,116/month — 0.19% of revenue", "Finance", "Projected"],
                ["Revenue streams", "Professional + Student + Business subscriptions + per-request fees", "Finance", "Designed"],
                ["Founder Programme cost", "Forgone subscription revenue from first 300 Professionals — bootstrap investment", "Finance", "Approved"],
                ["Break-even target", "Modelled in Budget Plan ILLM-04-006", "Finance", "Planned"],
            ],
        },
        "5. Operational Feasibility": {
            "narrative": "Phase 1 build is deliverable by the sole Software Engineer & Architect. Operations scale up with subscriber volume. Support Portal (Phase 5) introduces a dedicated support workspace; until then, operational support is handled via the Admin Portal.",
            "rows": [
                ["Phase 1 delivery", "Sole architect — feasible with focused build sprints", "Architect", "Confirmed"],
                ["Support scaling", "Admin Portal handles pre-Phase 5; Support Portal Phase 5 onwards", "Operations", "Designed"],
                ["DevOps maturity", "Standard K8s/Argo CD/Grafana stack — well-documented operations", "DevOps", "Confirmed"],
                ["Skill requirements", ".NET dev + DevOps fluency in K8s and Keycloak", "Architect", "Required"],
            ],
        },
        "6. Schedule Feasibility": {
            "narrative": "Eight-phase plan per spec §27 with phase estimates totalling 33–47 weeks. Schedule supports an MVP launch (Phase 1) within 8–10 weeks of authorisation.",
            "rows": [
                ["Phase 1 (Core + Auto-Application + Founder)", "8–10 weeks", "PM", "Estimate"],
                ["Phase 2 (Reporting/Payments/Notifications/Subscriptions)", "6–8 weeks", "PM", "Estimate"],
                ["Phase 3 (Privacy/Compliance/Admin)", "4–6 weeks", "PM", "Estimate"],
                ["Phase 4 (Internal Recruitment)", "3–4 weeks", "PM", "Estimate"],
                ["Phase 5 (Student CSR + Social + Support)", "4–6 weeks", "PM", "Estimate"],
                ["Phase 6 (AI Assistant + Weighting + Gap + Assets + PWA + Badges)", "5–7 weeks", "PM", "Estimate"],
                ["Phase 7 (Video)", "4–5 weeks", "PM", "Premium"],
                ["Phase 8 (RLHF + Marketplace)", "TBD", "PM", "Future"],
            ],
        },
        "7. Legal and Compliance Feasibility": {
            "narrative": "Namibian legal framework supports the platform's operation. Specific compliance concerns (Labour Act 11/2007, Electronic Transactions Act 4/2019, PCI-DSS, Constitution Article 10 anti-discrimination) are addressed in the compliance pack (folder 09) and require attorney review of disclaimer wording before launch.",
            "rows": [
                ["Labour Act 11/2007", "Platform is not a labour broker — disclaimers D-01/D-11 confirm", "Legal", "Pending attorney"],
                ["Electronic Transactions Act 4/2019", "Consent mechanisms in D-05/D-06 designed for compliance", "Legal", "Pending attorney"],
                ["Constitution Article 10", "Anti-discrimination controls D-04 + admin approval flow", "Legal", "Designed"],
                ["PCI-DSS", "SAQ A only — hosted gateway model; no card data on platform", "Compliance", "Confirmed"],
                ["Data protection", "Section 31 branding compliance + consent text; full GDPR-equivalent posture", "Legal", "Designed"],
            ],
        },
        "8. Conclusion and Recommendation": {
            "narrative": "All four feasibility dimensions assessed favourable. Recommend authorising Phase 1 build per the v3.7 corrected specification, subject to attorney sign-off on the disclaimer pack before go-live.",
            "rows": [
                ["Authorise Phase 1", "Per v3.7 corrected spec", "Sponsor", "Pending"],
                ["Condition — attorney sign-off", "Disclaimer pack D-01 through D-12 reviewed and approved before launch", "Sponsor", "Required"],
                ["Condition — gateway selection", "Confirm Namibian gateway and PCI-DSS SAQ A attestation", "Architect", "Required"],
            ],
        },
    },
}

BATCH3["01_Project_Initiation/Stakeholder_Register/ILLM-01-003_Stakeholder_Register_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 product model — five customer roles plus internal stakeholders.",
    "sections": {
        "2. Purpose": {
            "narrative": "This register identifies all stakeholders with an interest in or influence over the Illumin360 platform, their needs, and the engagement strategy for each.",
            "rows": [["Purpose", "Map stakeholders and engagement", "PM", "Active"]],
        },
        "3. Stakeholder Identification": {
            "narrative": "Stakeholders grouped by role and influence.",
            "rows": [
                ["Project Sponsor", "CEO, Illumin Investments CC — decisive authority", "Internal", "Primary"],
                ["Software Engineer & Architect", "Uriel Tapiwa Munjanga — sole technical owner", "Internal", "Primary"],
                ["Professionals (customer)", "Subscribed individuals seeking discoverability", "External", "Primary"],
                ["Students (customer)", "Free CSR-tier candidates", "External", "Primary"],
                ["Businesses (customer)", "Subscribing organisations seeking talent", "External", "Primary"],
                ["Founding Partners", "First 50 Business + 300 Professional accounts — early adopters", "External", "High influence"],
                ["University partners", "UNAM, NUST, IUM — student verification + spotlights", "External", "Important"],
                ["Anthropic", "Claude Sonnet 4.6 API provider (internal-only naming)", "External vendor", "Critical"],
                ["Google Cloud", "Cloud Vision OCR provider (internal-only naming)", "External vendor", "Important"],
                ["Payment gateway provider", "Hosted-page card processing — TBD selection", "External vendor", "Critical"],
                ["Namibian attorney", "Disclaimer pack and compliance review", "External", "Critical pre-launch"],
                ["Tax authority", "VAT compliance, transaction records", "External", "Regulatory"],
                ["Labour Commissioner", "Recruitment compliance overseer", "External", "Regulatory"],
            ],
        },
        "4. Stakeholder Analysis": {
            "narrative": "Power/interest grid analysis. Customers have high interest, lower direct power; sponsor and architect have high power. Vendors are critical dependencies. Regulators have power and moderate interest.",
            "rows": [
                ["High power / High interest — manage closely", "Sponsor, Architect, Founding Partners", "Marketing/PM", "Primary"],
                ["High power / Lower interest — keep satisfied", "Attorney, payment gateway provider, regulators", "PM/Legal", "Important"],
                ["Lower power / High interest — keep informed", "Customers (all 3 segments), university partners", "Marketing", "Engaged"],
                ["Lower power / Lower interest — monitor", "Industry observers, general public", "Marketing", "Watch"],
            ],
        },
        "5. Engagement Strategy": {
            "narrative": "Per-stakeholder engagement cadence.",
            "rows": [
                ["Sponsor", "Weekly status update + monthly strategic review", "PM", "Scheduled"],
                ["Architect", "Continuous (sole technical role)", "—", "Active"],
                ["Customers", "Marketing campaigns + AI Assistant + Support Portal + KB", "Marketing", "Designed"],
                ["Founding Partners", "Dedicated comms ahead of launch + permanent recognition", "Marketing", "Planned"],
                ["University partners", "Quarterly stakeholder calls + co-branded student materials", "Marketing", "Planned"],
                ["Attorney", "Disclaimer pack review pre-launch + annual review", "Legal", "Scheduled"],
                ["Vendors", "SLA review quarterly + escalation paths documented", "DevOps", "Scheduled"],
                ["Regulators", "Annual reporting + ad hoc as required", "Compliance", "Planned"],
            ],
        },
        "6. Communication Requirements": {
            "narrative": "Communication channels per stakeholder cluster.",
            "rows": [
                ["Status reports", "Weekly to sponsor; per-phase to broader audience", "PM", "Required"],
                ["Customer comms", "Email + in-product notifications + AI Assistant + KB articles", "Marketing", "Required"],
                ["Vendor comms", "Direct support channels per SLA", "DevOps", "Required"],
                ["Public comms", "Website, social, press releases (Section 31 branding compliant)", "Marketing", "Required"],
            ],
        },
        "7. Review and Approval": {
            "narrative": "Register reviewed quarterly and on material stakeholder changes.",
            "rows": [
                ["Quarterly review", "Refresh stakeholder roster and engagement", "PM", "Scheduled"],
                ["Sponsor sign-off", "Annually", "Sponsor", "Annual"],
            ],
        },
    },
}

BATCH3["01_Project_Initiation/Project_Charter/ILLM-01-004_Project_Charter_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec. The charter formalises authority, scope, deliverables, timeline, budget, and governance.",
    "sections": {
        "2. Project Overview": {
            "narrative": "Illumin360 is the Illumin Investments CC talent matching and recruitment platform for Namibia. It serves three customer segments (Professionals, Students, Businesses) through five portals (Professional, Student, Business, Administrator, Support) with the Auto-Application Engine as the defining feature.",
            "rows": [
                ["Project name", "Illumin360 Talent Match & Recruitment Platform", "—", "Confirmed"],
                ["Owner", "Illumin Investments CC (CC 2016/08234, VAT 07851437-015)", "—", "Confirmed"],
                ["Jurisdiction", "Namibia", "—", "Confirmed"],
                ["Sponsor", "CEO, Illumin Investments CC", "—", "Confirmed"],
                ["Architect", "Uriel Tapiwa Munjanga", "—", "Confirmed"],
            ],
        },
        "3. Project Scope": {
            "narrative": "Scope covers the build of all 5 portals, 13 microservices, and Phase 1 through Phase 7 of the 8-phase delivery plan. Phase 8 (RLHF model refinement + marketplace expansion) is out of scope for this charter and will be re-chartered when ready.",
            "rows": [
                ["In scope — portals", "5 portals: Professional, Student, Business, Administrator, Support", "Architect", "Confirmed"],
                ["In scope — phases", "Phase 1 through Phase 7", "PM", "Confirmed"],
                ["In scope — segments", "3 customer segments — Professionals, Students, Businesses", "Marketing", "Confirmed"],
                ["Out of scope — Phase 8", "RLHF model refinement; marketplace expansion — separate charter", "PM", "Deferred"],
                ["Out of scope — mobile native", "PWA covers; native apps not in initial scope", "PM", "Deferred"],
                ["Out of scope — ATS integration", "Not in initial scope", "PM", "Deferred"],
            ],
        },
        "4. Project Deliverables": {
            "narrative": "High-level deliverables. Detailed deliverables per phase in the Phase docs (folder 06).",
            "rows": [
                ["134 SDLC documents populated to v2.0", "All folders 01–14", "Architect", "In progress"],
                ["Phase 1 build — Core Talent Pool + Auto-Application + Founder", "Per ILLM-06-001 v2.0", "Architect", "Pending"],
                ["Phase 2 build — Reporting/Payments/Notifications/Subscriptions", "Per ILLM-06-002 v2.0", "Architect", "Pending"],
                ["Phases 3–7", "Per ILLM-06-003 through ILLM-06-007", "Architect", "Pending"],
                ["Attorney-signed disclaimer pack", "All 12 disclaimers reviewed before launch", "Legal", "Pending"],
                ["Production deployment", "Per ILLM-10-001 Deployment Plan v2.0", "DevOps", "Pending"],
            ],
        },
        "5. Project Timeline": {
            "narrative": "Eight-phase plan totalling 33–47 weeks per spec §27.",
            "rows": [
                ["Authorisation", "On charter approval", "Sponsor", "Trigger"],
                ["Phase 1", "Weeks 1–10", "Architect", "Estimate"],
                ["Phase 2", "Weeks 11–18", "Architect", "Estimate"],
                ["Phase 3", "Weeks 19–24", "Architect", "Estimate"],
                ["Phase 4", "Weeks 25–28", "Architect", "Estimate"],
                ["Phase 5", "Weeks 29–34", "Architect", "Estimate"],
                ["Phase 6", "Weeks 35–41", "Architect", "Estimate"],
                ["Phase 7 (premium)", "Weeks 42–46", "Architect", "Optional"],
            ],
        },
        "6. Project Budget": {
            "narrative": "Detailed in ILLM-04-006 Budget Plan. Operating costs dominated by infrastructure and AI services. Build cost is sole-architect time plus required tooling.",
            "rows": [
                ["AI services Year 1", "≈ NAD 77/month", "Finance", "Projected"],
                ["Infrastructure", "Hosting + Kubernetes capacity — TBC", "DevOps", "Estimate"],
                ["Build cost", "Architect time + tooling licences (mostly OSS — minimal)", "Finance", "Internal"],
                ["External pentest pre-launch", "Required — TBC vendor cost", "Security", "Required"],
                ["Attorney review", "Required — TBC fee", "Legal", "Required"],
            ],
        },
        "7. Project Team": {
            "narrative": "Sole technical owner plus extended stakeholders.",
            "rows": [
                ["Architect (sole technical)", "Uriel Tapiwa Munjanga", "—", "Active"],
                ["Sponsor", "CEO, Illumin Investments CC", "—", "Active"],
                ["External attorney", "TBD — Namibian", "—", "Engaged when ready"],
                ["External security tester", "TBD pre-launch", "—", "Engaged when ready"],
                ["Operations (post-launch)", "To recruit — Support staff for Phase 5", "—", "Planned"],
            ],
        },
        "8. Governance Structure": {
            "narrative": "Governance follows the Sponsor/Architect/Stakeholder model. Steering Committee, Project Team, and Client Reviews each have their own meeting minutes templates (ILLM-13-001 through ILLM-13-003).",
            "rows": [
                ["Sponsor", "Decision authority, budget approval, strategic direction", "—", "Active"],
                ["Architect", "Technical authority, design, build, deployment", "—", "Active"],
                ["Project Team meetings", "Per ILLM-13-002 template", "PM", "Per-phase"],
                ["Steering Committee", "Per ILLM-13-001 template — quarterly", "PM", "Quarterly"],
                ["Change Control", "Per ILLM-11-005 and ILLM-13-005", "PM", "Continuous"],
            ],
        },
        "9. Roles and Responsibilities": {
            "narrative": "RACI for key activities.",
            "rows": [
                ["Authorise budget", "Sponsor (A); Architect (R/C); — (I)", "—", "RACI"],
                ["Approve architecture", "Architect (R/A); Sponsor (C/I)", "—", "RACI"],
                ["Approve disclaimer pack", "Attorney (A); Architect (R); Sponsor (I)", "—", "RACI"],
                ["Sign off Phase increments", "Sponsor (A); Architect (R)", "—", "RACI"],
                ["Operations post-launch", "Operations (R); Sponsor (A); Architect (C)", "—", "RACI"],
            ],
        },
        "10. Approval": {
            "narrative": "Charter signed by Sponsor authorises the build to commence.",
            "rows": [
                ["Sponsor", "Authorise project", "CEO Illumin Investments CC", "Pending"],
                ["Architect", "Accept technical responsibility", "Software Engineer & Architect", "Drafted"],
            ],
        },
    },
}

BATCH3["01_Project_Initiation/Initial_Risk_Register/ILLM-01-005_Initial_Risk_Register_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 risk assessment. Top-tier risks at initiation; ongoing register in ILLM-13-007.",
    "sections": {
        "2. Purpose": {"narrative": "Captures top-tier risks at project initiation. Live register maintained in ILLM-13-007.", "rows": [["Purpose", "Initial risk identification", "PM", "Active"]]},
        "3. Risk Management Approach": {
            "narrative": "Risks classified by category (Market, Technical, Compliance, Operational, Commercial, Vendor) with probability × impact scoring. Mitigation owner identified per risk. Reviewed weekly during build; monthly post-launch.",
            "rows": [
                ["Scoring", "Probability (L/M/H) × Impact (L/M/H)", "PM", "Standard"],
                ["Review cadence — build", "Weekly", "PM", "Active"],
                ["Review cadence — operate", "Monthly", "PM", "Planned"],
                ["Escalation", "High impact + High probability → immediate to Sponsor", "PM", "Standard"],
            ],
        },
        "4. Risk Categories": {
            "narrative": "Six risk categories track distinct concerns.",
            "rows": [
                ["Market", "Demand assumption, competitive response, customer acquisition", "Marketing", "Tracked"],
                ["Technical", "Architecture choices, performance targets, vendor stability", "Architect", "Tracked"],
                ["Compliance", "Labour Act, ETA, PCI-DSS, anti-discrimination", "Legal", "Tracked"],
                ["Operational", "Sole-architect bandwidth, ops scaling, incident response", "Operations", "Tracked"],
                ["Commercial", "Subscription pricing, revenue model, Founder Programme cost", "Finance", "Tracked"],
                ["Vendor", "Anthropic, Google, payment gateway dependencies", "Architect", "Tracked"],
            ],
        },
        "5. Risk Register Table": {
            "narrative": "Top-tier risks at project initiation.",
            "rows": [
                ["R-01 Talent pool cold start", "Insufficient candidates at launch — Auto-Application has nothing to surface", "M × H", "Founder Programme + university partnerships"],
                ["R-02 Founder quota race", "Concurrent registrations both claim slot 300", "L × M", "SELECT FOR UPDATE serialisation"],
                ["R-03 AI vendor outage", "Claude API unavailable mid-shortlist", "L × H", "Keyword + template fallback; Phase 8 Ollama"],
                ["R-04 Branding leak", "Client-facing content references Claude/Anthropic", "M × M", "Post-gen filter + quarterly audit (Section 31)"],
                ["R-05 Photo blind-screening bypass", "Photo accidentally included in shortlist projection", "L × H", "Structural — column not in projection query"],
                ["R-06 Discriminatory filter use", "Business uses sensitive filter without lawful basis", "M × H", "D-04 warning + 50-word justification + admin approval"],
                ["R-07 Auto-Application consent challenge", "Candidate disputes auto-consideration without explicit apply", "M × M", "Explicit consent text + per-segment opt-outs"],
                ["R-08 Attorney delay", "Disclaimer review delays launch", "M × M", "Early engagement; parallel work on non-legal scope"],
                ["R-09 Sole-architect bandwidth", "Build slips due to single owner", "M × H", "Phase 1 scope tight; defer non-essential to later phases"],
                ["R-10 Payment gateway selection", "Gateway selection delayed or selected gateway lacks features", "M × M", "Early gateway evaluation; SAQ A model is portable"],
                ["R-11 University partnership delay", "Institutional verification programme delayed", "M × M", "Method 2 (admin review) and Method 3 (uploaded letter) as fallback"],
                ["R-12 Cost overrun on AI", "Usage exceeds projections", "L × M", "Monthly cost monitoring + 200% YoY alert"],
                ["R-13 Compliant Recruiter dispute", "Business disputes badge revocation", "L × L", "90-day cooling-off + documented admin reason"],
                ["R-14 Internal portal abuse", "Public token brute-forced", "L × M", "Tokenised URL + closing-time auto-lock + rate limit"],
                ["R-15 Webhook spoofing", "Forged payment confirmation", "L × H", "HMAC signature verification mandatory before processing"],
            ],
        },
        "6. Assumptions and Constraints": {"narrative": "Risk assessment assumes Founder demand, vendor stability, and attorney availability.", "rows": [["Assumption", "Demand sufficient to reach Founder quotas", "Marketing", "To validate"]]},
        "7. Review and Approval": {"narrative": "Reviewed by Sponsor at initiation; live register thereafter.", "rows": [["Sponsor approval", "Initial register accepted", "Sponsor", "Pending"]]},
    },
}

BATCH3["01_Project_Initiation/Sign_Off/ILLM-01-006_Project_Initiation_Sign_Off_v1_0.docx"] = {
    "v2_change_description": "Sign-off record for Phase 01 Initiation completion.",
    "sections": {
        "2. Sign-Off Scope": {
            "narrative": "This sign-off authorises closure of the Project Initiation phase. It certifies that the Business Case, Feasibility Study, Stakeholder Register, Project Charter, and Initial Risk Register have been completed and reviewed.",
            "rows": [
                ["ILLM-01-001 Business Case", "Populated to v2.0", "Architect", "Drafted"],
                ["ILLM-01-002 Feasibility Study", "Populated to v2.0", "Architect", "Drafted"],
                ["ILLM-01-003 Stakeholder Register", "Populated to v2.0", "Architect", "Drafted"],
                ["ILLM-01-004 Project Charter", "Populated to v2.0", "Architect", "Drafted"],
                ["ILLM-01-005 Initial Risk Register", "Populated to v2.0", "Architect", "Drafted"],
            ],
        },
        "3. Document Review Summary": {
            "narrative": "All Phase 01 documents reviewed against the v3.7 corrected spec. Pending sponsor review.",
            "rows": [["All five docs", "Drafted, internally consistent, traceable to v3.7 spec", "Architect", "Drafted"]],
        },
        "4. Conditions and Actions": {
            "narrative": "Conditions to be met before phase closure.",
            "rows": [
                ["Sponsor sign-off on Business Case", "Pending", "Sponsor", "Outstanding"],
                ["v3.7 spec amendment merged into master", "Pending", "Architect", "Outstanding"],
                ["Initial Risk Register accepted", "Pending", "Sponsor", "Outstanding"],
            ],
        },
        "5. Authorising Signatures": {"narrative": "Required signatures for Phase 01 closure.", "rows": [["Sponsor", "Authorise Phase 01 closure", "CEO Illumin Investments CC", "Pending"], ["Architect", "Confirm completeness", "Software Engineer & Architect", "Drafted"]]},
        "6. Distribution List": {"narrative": "Distribution of the signed-off Phase 01 pack.", "rows": [["Sponsor", "Master copy", "—", "Required"], ["Architect", "Working copy", "—", "Active"], ["Legal", "Compliance reference", "—", "Required"]]},
    },
}

# ───────────────── Folder 02 — Requirements completion ───────────────────────

BATCH3["02_Requirements/Compliance_Legal_Requirements/ILLM-02-005_Compliance_Legal_Requirements_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec compliance posture and the 12-disclaimer pack.",
    "sections": {
        "2. Purpose": {"narrative": "Specifies compliance and legal requirements the platform must satisfy.", "rows": [["Purpose", "Capture compliance posture", "Legal", "Active"]]},
        "3. Legal Framework": {
            "narrative": "Namibian legal framework applicable to the platform.",
            "rows": [
                ["Labour Act 11 of 2007", "Recruitment not labour broking — disclaimers D-01/D-11", "Legal", "Required"],
                ["Constitution Article 10", "Anti-discrimination — D-03/D-04 enforcement", "Legal", "Required"],
                ["Electronic Transactions Act 4 of 2019", "Consent mechanisms D-05/D-06", "Legal", "Required"],
                ["Affirmative Action (Employment) Act 29 of 1998", "Sensitive filter lawful justification grounds", "Legal", "Required"],
                ["Income Tax Act", "Financial record retention 7 years", "Compliance", "Required"],
                ["PCI-DSS SAQ A", "Hosted gateway payment model", "Compliance", "Required"],
            ],
        },
        "4. Compliance Requirements": {
            "narrative": "Detailed compliance requirements grouped by area.",
            "rows": [
                ["CR-1 Labour-broker exclusion", "Disclaimers D-01 and D-11 must be displayed", "Legal", "Mandatory"],
                ["CR-2 Anti-discrimination", "D-03 declaration mandatory; D-04 sensitive-filter warning + justification + admin approval", "Legal", "Mandatory"],
                ["CR-3 Automated processing transparency", "D-02 + D-05 + D-07 disclose automated decision-making", "Legal", "Mandatory"],
                ["CR-4 Consent collection", "D-05 + D-06 + Auto-Application clause; ticked checkbox required", "Legal", "Mandatory"],
                ["CR-5 Data retention", "D-12 retention schedule enforced; audit logs 7 years", "Compliance", "Mandatory"],
                ["CR-6 PCI-DSS SAQ A", "No card data on platform; quarterly ASV scan; annual SAQ attestation", "Compliance", "Mandatory"],
                ["CR-7 Audit immutability", "audit_logs and compliance_justifications append-only", "Architect", "Mandatory"],
                ["CR-8 Section 31 branding", "Client-facing content free of AI/vendor references", "Marketing", "Mandatory"],
                ["CR-9 Blind screening", "Photo not in matching engine or shortlist projection", "Architect", "Mandatory"],
            ],
        },
        "5. Compliance Checklist": {
            "narrative": "Pre-launch compliance checklist.",
            "rows": [
                ["12 disclaimers reviewed by attorney", "All D-01 through D-12 signed off", "Legal", "Pending"],
                ["Consent UI tested", "Checkbox enforced; unticked = blocked submission", "QA", "Pending"],
                ["Sensitive filter flow tested", "D-04 + justification + admin approval end-to-end", "QA", "Pending"],
                ["Audit log immutability test", "No UI delete path; row attempts rejected", "QA", "Pending"],
                ["Branding audit", "Quarterly client-facing scan for AI/vendor terms", "Marketing", "Scheduled"],
                ["Photo blind-screen test", "Photo absent in all preview and shortlist responses", "QA", "Pending"],
                ["PCI-DSS SAQ A attestation", "Submitted to acquirer", "Compliance", "Pending"],
                ["Data retention policy enforced", "Cron-based purge per D-12", "Architect", "Pending"],
            ],
        },
        "6. Attorney Notes": {"narrative": "Held in the disclaimer master file and the attorney review pack.", "rows": [["See Disclaimer Master v3.7", "Attorney notes consolidated", "Legal", "Reference"]]},
        "7. Assumptions and Constraints": {"narrative": "Compliance posture assumes Namibian jurisdiction; multi-jurisdiction expansion requires legal review per country.", "rows": [["Jurisdiction — Namibia", "Initial launch market", "Legal", "Confirmed"]]},
        "8. Approval": {"narrative": "Compliance pack sign-off.", "rows": [["Attorney", "Sign-off on full disclaimer pack", "External attorney", "Pending"], ["Sponsor", "Accept compliance posture", "Sponsor", "Pending"]]},
    },
}

BATCH3["02_Requirements/Requirements_Traceability_Matrix/ILLM-02-006_Requirements_Traceability_Matrix_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec — top-level traceability across BRs, FRs, design artefacts, test cases.",
    "sections": {
        "2. Purpose": {"narrative": "End-to-end requirement traceability from business intent to test verification.", "rows": [["Purpose", "Trace BR → FR → design → test", "Architect", "Active"]]},
        "3. Scope": {"narrative": "Covers all FR families (FR-1 through FR-39) and their upstream BRs and downstream design + tests.", "rows": [["Scope", "All FR families", "Architect", "Defined"]]},
        "4. Requirements Traceability Matrix": {
            "narrative": "Top-level matrix. Full row-per-requirement matrix maintained separately.",
            "rows": [
                ["BR-X-1 (Auto-Application)", "FR-24 — ILLM-03-013 Auto-Application Design (renumber as ILLM-03-022) — Test pack TC-AA-*", "Architect", "Traced"],
                ["BR-X-2 (Founder)", "FR-21 — ILLM-03-011 Founder Programme Design — TC-FN-*", "Architect", "Traced"],
                ["BR-P-1 (Discoverability)", "FR-2, FR-24 — ILLM-03-013 + ILLM-03-022 — TC-P-*", "Architect", "Traced"],
                ["BR-B-1 (Shortlist)", "FR-4, FR-6, FR-7 — ILLM-03-005 + ILLM-03-009 — TC-B-*", "Architect", "Traced"],
                ["BR-B-2 (Benchmarking)", "FR-25 — Benchmarking Design (new) — TC-BM-*", "Architect", "Traced"],
                ["BR-B-3 (Internal recruitment)", "FR-11 — ILLM-06-004 + ILLM-03-005 — TC-IR-*", "Architect", "Traced"],
                ["BR-B-4 (Business subscription)", "FR-3, FR-28 — Business Subscription Design (new) — TC-BS-*", "Architect", "Traced"],
                ["BR-S-1 (Student CSR)", "FR-12 — ILLM-06-005 — TC-S-*", "Architect", "Traced"],
                ["BR-A-1 (Admin)", "FR-10 — ILLM-03-005 admin endpoints — TC-A-*", "Architect", "Traced"],
                ["BR-SU-1 (Support)", "FR-23 — ILLM-03-013 Support Portal — TC-SU-*", "Architect", "Traced"],
            ],
        },
        "5. Coverage Analysis": {"narrative": "Coverage by phase.", "rows": [["Phase 1 FRs", "All traced to design + test pack", "Architect", "Covered"], ["Phase 2 FRs", "All traced", "Architect", "Covered"], ["Phase 3–7 FRs", "Traced; tests pending build", "Architect", "Designed"]]},
        "6. Gap Analysis": {"narrative": "No coverage gaps at FR → design level. Test pack coverage gaps tracked per phase.", "rows": [["Design coverage", "100% — all FRs have a design artefact", "Architect", "Met"], ["Test coverage", "Phase 1 tests pending build", "QA", "In progress"]]},
        "7. Review and Approval": {"narrative": "Matrix reviewed at every phase gate.", "rows": [["Phase-gate review", "Architect + QA Lead", "—", "Per phase"]]},
    },
}

BATCH3["02_Requirements/Stakeholder_Sign_Off/ILLM-02-007_Requirements_Sign_Off_v1_0.docx"] = {
    "v2_change_description": "Sign-off record for Phase 02 Requirements completion.",
    "sections": {
        "2. Sign-Off Scope": {
            "narrative": "Authorises closure of the Requirements phase. Covers BR, FR, NFR, Use Cases, Compliance, and RTM documents.",
            "rows": [
                ["ILLM-02-001 Business Requirements", "Populated v2.0", "Architect", "Drafted"],
                ["ILLM-02-002 Functional Requirements", "Populated v2.0", "Architect", "Drafted"],
                ["ILLM-02-003 Non-Functional Requirements", "Populated v2.0", "Architect", "Drafted"],
                ["ILLM-02-004 Use Cases / User Stories", "Populated v2.0", "Architect", "Drafted"],
                ["ILLM-02-005 Compliance & Legal Requirements", "Populated v2.0", "Architect", "Drafted"],
                ["ILLM-02-006 Requirements Traceability Matrix", "Populated v2.0", "Architect", "Drafted"],
            ],
        },
        "3. Document Review Summary": {"narrative": "All Phase 02 documents drafted to v2.0. Pending stakeholder review.", "rows": [["All six docs", "Internally consistent, traced to v3.7 spec", "Architect", "Drafted"]]},
        "4. Conditions and Actions": {"narrative": "Conditions for closure.", "rows": [["Sponsor sign-off", "Pending", "Sponsor", "Outstanding"], ["Marketing review", "Pending", "Marketing", "Outstanding"], ["Legal sign-off on compliance reqs", "Pending", "Legal", "Outstanding"]]},
        "5. Authorising Signatures": {"narrative": "Required signatures.", "rows": [["Sponsor", "Phase 02 closure", "CEO", "Pending"], ["Architect", "Completeness", "Software Engineer & Architect", "Drafted"], ["Legal", "Compliance acceptance", "External attorney", "Pending"]]},
        "6. Distribution List": {"narrative": "Distribution.", "rows": [["Sponsor", "Master", "—", "Required"], ["Architect", "Working", "—", "Active"], ["QA Lead", "Test plan basis", "—", "Required"]]},
    },
}

# ───────────────── Folder 09 — Remaining 11 Disclaimers + Compliance ─────────

# Common disclaimer doc structure: 2.Purpose, 3.Disclaimer Text, 4.Implementation Location, 5.Attorney Review Status, 6.Attorney Notes
_DISCLAIMER_NARRATIVES = {
    "D-01": ("Platform Technology Disclaimer", "Establish clearly that Illumin360 is a technology platform — not a recruitment agency or labour broker.", "Employer dashboard header banner; employer login page below login form; persistent — cannot be dismissed."),
    "D-03": ("Employer Compliance Declaration", "Require the Business to confirm fair, non-discriminatory selection criteria before each request is submitted.", "Recruitment request submission — Step 4 (Review & Submit). Checkbox must be ticked; cannot be pre-ticked."),
    "D-04": ("Sensitive Filter Legal Warning", "Display a legal warning when gender or age is selected as a filter; require ≥50-word justification.", "Recruitment request form — Filters section. Auto-displayed when sensitive filter selected; cannot be dismissed."),
    "D-05": ("Candidate Data Consent Statement", "Capture explicit consent for data collection, profile visibility, automated processing, and the Auto-Application standing-application principle.", "Professional registration — final step. Checkbox must be ticked; registration blocked until ticked."),
    "D-06": ("Student Registration Consent", "Capture additional consent specific to Student CSR — verification, institution data sharing, graduate upgrade.", "Student registration — second checkbox after D-05; both required."),
    "D-07": ("Internal Recruitment Portal Applicant Notice", "Notify internal applicants of automated processing and the limited use of their data for the specific vacancy.", "Public-facing internal recruitment portal page — above the application form; persistent."),
    "D-08": ("CV Processing Notice", "Acknowledge that uploaded CVs are stored encrypted and processed automatically without human review in normal operation.", "CV upload confirmation screen; modal or inline notice."),
    "D-09": ("Candidate Profile Unlock Notice", "Confirm permitted use, data protection, no-guarantee, and non-refundable nature of candidate unlock fees.", "Candidate unlock payment modal — before payment initiation."),
    "D-10": ("Platform Email Footer Disclaimer", "Reiterate platform-not-agency framing and confidentiality on every system-generated email.", "Auto-appended to footer of every system email."),
    "D-11": ("Website General Disclaimer", "Public website general disclaimer reiterating platform-not-agency framing and limitations of liability.", "Website footer on every page; About page; Terms of Service page."),
    "D-12": ("Data Retention Notice", "Explain personal data retention periods per data category.", "Account settings page; Privacy Policy page."),
}

for code, (title, purpose, location) in _DISCLAIMER_NARRATIVES.items():
    seq = code.replace("D-", "")
    if int(seq) >= 2:
        path = f"09_Compliance_Legal/Disclaimers/D{seq.zfill(2)}_{title.replace(' ','_')}/ILLM-09-{seq.zfill(3)}_D{seq.zfill(2)}_{title.replace(' ','_').replace('Disclaimer','Disclaimer').replace('_Disclaimer','')}_v1_0.docx"

# Use the exact known filenames
DISCLAIMER_PATHS = {
    "D-01": "09_Compliance_Legal/Disclaimers/D01_Platform_Technology/ILLM-09-001_D01_Platform_Technology_Disclaimer_v1_0.docx",
    "D-03": "09_Compliance_Legal/Disclaimers/D03_Employer_Compliance_Declaration/ILLM-09-003_D03_Employer_Compliance_Declaration_v1_0.docx",
    "D-04": "09_Compliance_Legal/Disclaimers/D04_Sensitive_Filter_Warning/ILLM-09-004_D04_Sensitive_Filter_Warning_v1_0.docx",
    "D-05": "09_Compliance_Legal/Disclaimers/D05_Candidate_Data_Consent/ILLM-09-005_D05_Candidate_Data_Consent_v1_0.docx",
    "D-06": "09_Compliance_Legal/Disclaimers/D06_Student_Registration_Consent/ILLM-09-006_D06_Student_Registration_Consent_v1_0.docx",
    "D-07": "09_Compliance_Legal/Disclaimers/D07_Internal_Portal_Notice/ILLM-09-007_D07_Internal_Portal_Notice_v1_0.docx",
    "D-08": "09_Compliance_Legal/Disclaimers/D08_CV_Processing_Notice/ILLM-09-008_D08_CV_Processing_Notice_v1_0.docx",
    "D-09": "09_Compliance_Legal/Disclaimers/D09_Candidate_Unlock_Notice/ILLM-09-009_D09_Candidate_Unlock_Notice_v1_0.docx",
    "D-10": "09_Compliance_Legal/Disclaimers/D10_Email_Footer/ILLM-09-010_D10_Email_Footer_Disclaimer_v1_0.docx",
    "D-11": "09_Compliance_Legal/Disclaimers/D11_Website_General/ILLM-09-011_D11_Website_General_Disclaimer_v1_0.docx",
    "D-12": "09_Compliance_Legal/Disclaimers/D12_Data_Retention/ILLM-09-012_D12_Data_Retention_Notice_v1_0.docx",
}

for code, path in DISCLAIMER_PATHS.items():
    title, purpose, location = _DISCLAIMER_NARRATIVES[code]
    BATCH3[path] = {
        "v2_change_description": f"Populated from v3.7 disclaimer master (corrected). {title} ({code}) per Section 31 branding policy compliance.",
        "sections": {
            "2. Purpose": {
                "narrative": purpose,
                "rows": [
                    ["Purpose", purpose, "Legal", "Required"],
                    ["Audience", f"Recipients/viewers per location ({location[:60]}…)", "Marketing", "—"],
                    ["Branding", "Section 31 — Illumin360 branding; no third-party-vendor mentions", "Marketing", "Mandatory"],
                ],
            },
            "3. Disclaimer Text": {
                "narrative": f"Verbatim text per v3.7 disclaimer master. To be reproduced exactly in the platform's UI / templates / email footer per location.",
                "rows": [
                    [f"{code} heading", title, "Marketing", "Approved"],
                    ["Source", "Illumin360 Disclaimer Master v3.7 (corrected)", "Legal", "Authoritative"],
                    ["Status", "Drafted — pending attorney review", "Legal", "Pending"],
                ],
            },
            "4. Implementation Location": {
                "narrative": location,
                "rows": [[f"{code} location", location, "Architect", "Spec'd"]],
            },
            "5. Attorney Review Status": {
                "narrative": "Disclaimer requires Namibian attorney sign-off before go-live.",
                "rows": [
                    ["Status", "Drafted — pending attorney review", "Legal", "Pending"],
                    ["Reviewer", "TBD — qualified Namibian attorney", "Legal", "Pending"],
                    ["Target sign-off", "Before first live use", "Sponsor", "Required"],
                ],
            },
            "6. Attorney Notes": {
                "narrative": "Attorney review questions are held in the disclaimer master file. Key questions for this disclaimer summarised below.",
                "rows": [
                    [f"{code} review question 1", "Confirm wording is enforceable under Namibian contract law", "Attorney", "Open"],
                    [f"{code} review question 2", "Confirm Section 31 branding-compatible wording is legally sufficient", "Attorney", "Open"],
                    [f"{code} review question 3", "Confirm any required cross-references to specific statutes", "Attorney", "Open"],
                ],
            },
        },
    }

# Non-disclaimer compliance docs in folder 09
BATCH3["09_Compliance_Legal/PCI_DSS_Compliance/ILLM-09-017_PCI_DSS_Compliance_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §11 and §17.1 — PCI-DSS SAQ A posture via hosted-gateway model.",
    "sections": {
        "2. Purpose": {"narrative": "Document the platform's PCI-DSS compliance posture — SAQ A through hosted-gateway model.", "rows": [["Purpose", "PCI-DSS SAQ A documentation", "Compliance", "Active"]]},
        "3. PCI-DSS Compliance": {
            "narrative": "Scope is restricted to SAQ A. No card data is captured, transmitted, processed, or stored on Illumin360 infrastructure. The payment gateway handles all card-data flows on its hosted page; the platform receives only token references and amounts via signed webhooks.",
            "rows": [
                ["Scope", "SAQ A only", "Compliance", "Confirmed"],
                ["Card data flow", "Direct to gateway hosted page; never touches platform", "Architect", "Confirmed"],
                ["Webhook content", "Token reference, status, amount, gateway reference — no PAN", "Architect", "Confirmed"],
                ["TLS for redirect endpoint", "TLS 1.2+", "DevOps", "Confirmed"],
                ["3DS support", "Required from gateway selection", "Architect", "Required"],
                ["Refund flow", "Initiated via gateway API; no card data handled", "payment-svc", "Designed"],
            ],
        },
        "4. PCI-DSS Controls": {
            "narrative": "SAQ A requires specific controls. The platform implements each.",
            "rows": [
                ["C-1 TLS on all card-data flow", "TLS 1.2+ on the redirect endpoint", "DevOps", "Implemented"],
                ["C-2 No card data storage", "No PAN, CVV, or full card detail stored", "Architect", "Implemented"],
                ["C-3 Gateway certification", "Gateway must be PCI-DSS certified", "Compliance", "Required"],
                ["C-4 Annual SAQ attestation", "Filed annually with acquirer", "Compliance", "Required"],
                ["C-5 Quarterly ASV scan", "Approved Scanning Vendor required", "DevOps", "Required"],
                ["C-6 Webhook HMAC", "Signed webhook verification mandatory", "Architect", "Implemented"],
                ["C-7 Idempotent webhook", "Replay protection by (gateway_ref, status)", "Architect", "Implemented"],
            ],
        },
        "5. Webhook Security": {
            "narrative": "Webhook security details.",
            "rows": [
                ["HMAC algorithm", "HMAC-SHA-256 minimum", "Architect", "Required"],
                ["Secret rotation", "Quarterly", "DevOps", "Required"],
                ["Replay protection", "(gateway_ref, status) UNIQUE — duplicates dropped", "Architect", "Implemented"],
                ["Timestamp window", "Reject webhook if timestamp > 5 minutes drift", "Architect", "Implemented"],
                ["Failure logging", "Failed HMAC verifications logged + admin alert on threshold breach", "Architect", "Implemented"],
            ],
        },
        "6. Compliance Checklist": {
            "narrative": "Pre-launch and ongoing checklist.",
            "rows": [
                ["Gateway selected and certified PCI-DSS", "Pending selection", "Architect", "Pending"],
                ["SAQ A submitted", "Annual", "Compliance", "Pending"],
                ["ASV scan run", "Quarterly", "DevOps", "Scheduled"],
                ["Webhook HMAC verified end-to-end", "QA", "Pending", "—"],
                ["No PAN in logs", "Verified pre-launch", "QA", "Pending"],
            ],
        },
        "7. Review and Approval": {"narrative": "Annual review and SAQ resubmission.", "rows": [["Annual SAQ resubmission", "Maintain attestation", "Compliance", "Annual"]]},
    },
}

BATCH3["09_Compliance_Legal/Labour_Act_Compliance/ILLM-09-014_Labour_Act_Compliance_v1_0.docx"] = {
    "v2_change_description": "Populated — Labour Act 11 of 2007 compliance posture.",
    "sections": {
        "2. Purpose": {"narrative": "Document compliance with Namibia Labour Act 11 of 2007.", "rows": [["Purpose", "Labour Act compliance posture", "Legal", "Active"]]},
        "3. Legal Framework": {"narrative": "Sections of the Labour Act 11/2007 relevant to the platform.", "rows": [["S.1 — definitions of labour broker / employment service provider", "Platform must be distinguishable from these", "Legal", "Critical"], ["Anti-discrimination provisions", "Aligned with Article 10 of the Constitution", "Legal", "Critical"], ["EEO provisions", "Sensitive filter D-04 protocol", "Legal", "Critical"]]},
        "4. Compliance Requirements": {"narrative": "Concrete requirements derived from the Act.", "rows": [["CR-LA-1 Platform-not-broker disclosure", "Disclaimers D-01 and D-11", "Legal", "Mandatory"], ["CR-LA-2 No worker placement", "Platform does not place workers — UI and operations preserve this", "Architect", "Mandatory"], ["CR-LA-3 No employment-intermediary role", "Platform owns no employment relationships", "Legal", "Mandatory"], ["CR-LA-4 Anti-discrimination controls", "D-03/D-04 + admin approval + audit", "Architect", "Mandatory"]]},
        "5. Compliance Checklist": {"narrative": "Pre-launch checklist.", "rows": [["Attorney review of D-01/D-11 wording", "Pending", "Legal", "Pending"], ["Anti-discrimination flow tested end-to-end", "QA", "Pending", "—"], ["Section 31 branding compliant", "Audited", "Marketing", "Pending"]]},
        "6. Attorney Notes": {"narrative": "Held in disclaimer master.", "rows": [["Attorney review pack", "Reference Disclaimer Master v3.7", "Legal", "Reference"]]},
        "7. Assumptions and Constraints": {"narrative": "Assumes Namibian jurisdiction only.", "rows": [["Jurisdiction", "Namibia", "—", "Confirmed"]]},
        "8. Approval": {"narrative": "Attorney sign-off pre-launch.", "rows": [["Attorney", "Sign-off", "External", "Pending"]]},
    },
}

BATCH3["09_Compliance_Legal/Data_Protection_GDPR_Equivalents/ILLM-09-015_Data_Protection_v1_0.docx"] = {
    "v2_change_description": "Populated — data protection posture aligned to Electronic Transactions Act 4/2019 and GDPR-equivalent best practices.",
    "sections": {
        "2. Purpose": {"narrative": "Document the platform's data protection posture aligned to ETA 4/2019 and GDPR-equivalent best practices.", "rows": [["Purpose", "Data protection documentation", "Legal", "Active"]]},
        "3. Legal Framework": {"narrative": "Namibia ETA 4/2019 plus GDPR-style principles applied as best practice.", "rows": [["ETA 4/2019", "Electronic consent mechanisms", "Legal", "Required"], ["GDPR principles (best practice)", "Lawful basis, minimisation, transparency, retention, rights", "Legal", "Adopted"], ["Section 31 branding", "Vendor names not exposed client-facing", "Marketing", "Required"]]},
        "4. Compliance Requirements": {"narrative": "Requirements derived.", "rows": [["DP-1 Lawful basis", "Consent collected D-05/D-06; legitimate interest documented for analytics", "Legal", "Mandatory"], ["DP-2 Minimisation", "Only data required for matching/compliance retained", "Architect", "Mandatory"], ["DP-3 Transparency", "Disclaimers D-05/D-07/D-12 explicit", "Legal", "Mandatory"], ["DP-4 Retention", "Per D-12 schedule", "Architect", "Mandatory"], ["DP-5 Rights — access/delete/export", "Self-service deletion + privacy@illumin360.com for access/export", "Support", "Mandatory"], ["DP-6 Sub-processors", "Anthropic + Google Cloud disclosed in consent", "Legal", "Required"], ["DP-7 International transfer", "Disclosed in D-05; data may be processed outside Namibia", "Legal", "Disclosed"]]},
        "5. Compliance Checklist": {"narrative": "Pre-launch checklist.", "rows": [["Consent UI tested", "Mandatory checkboxes enforced", "QA", "Pending"], ["DSAR process drafted", "Privacy@illumin360.com SLA defined", "Support", "Pending"], ["Retention purge tested", "Cron-based deletion per D-12", "QA", "Pending"]]},
        "6. Attorney Notes": {"narrative": "Held in disclaimer master.", "rows": [["Reference", "Disclaimer Master v3.7", "Legal", "Reference"]]},
        "7. Assumptions and Constraints": {"narrative": "Assumes ETA-equivalent baseline; future Namibian Data Protection Act expected to tighten.", "rows": [["Future regulation", "Monitor for Namibian Data Protection Act", "Legal", "Watch"]]},
        "8. Approval": {"narrative": "Attorney sign-off pre-launch.", "rows": [["Attorney", "Sign-off", "External", "Pending"]]},
    },
}

BATCH3["09_Compliance_Legal/Anti_Discrimination_Controls/ILLM-09-016_Anti_Discrimination_Controls_v1_0.docx"] = {
    "v2_change_description": "Populated — Article 10 (Constitution) + Labour Act 11/2007 + Affirmative Action Act 29/1998 controls.",
    "sections": {
        "2. Purpose": {"narrative": "Document anti-discrimination controls: declaration D-03, sensitive-filter D-04 protocol, blind screening, audit immutability.", "rows": [["Purpose", "Anti-discrimination posture", "Legal", "Active"]]},
        "3. Legal Framework": {"narrative": "Namibian framework.", "rows": [["Constitution Article 10", "Equality and non-discrimination", "Legal", "Critical"], ["Labour Act 11 of 2007", "Workplace anti-discrimination provisions", "Legal", "Critical"], ["Affirmative Action (Employment) Act 29 of 1998", "Lawful affirmative action grounds", "Legal", "Critical"]]},
        "4. Compliance Requirements": {"narrative": "Concrete controls.", "rows": [["AD-1 D-03 declaration", "Mandatory tick per request — false declaration grounds for suspension", "Architect", "Implemented"], ["AD-2 D-04 sensitive-filter warning", "Auto-displayed when gender/age selected", "Architect", "Implemented"], ["AD-3 50-word justification", "Mandatory before submission", "Architect", "Implemented"], ["AD-4 Admin approval gate", "Sensitive-filtered shortlists require admin approval before unlock", "Architect", "Implemented"], ["AD-5 Audit immutability", "compliance_justifications append-only; no UI delete", "Architect", "Mandatory"], ["AD-6 Blind screening", "Photo not in matching engine or shortlist projection", "Architect", "Structural"], ["AD-7 Justification engine", "Age and DOB never referenced in candidate analysis; gender only if lawfully filtered", "Architect", "Implemented"]]},
        "5. Compliance Checklist": {"narrative": "Tests.", "rows": [["D-04 flow tested end-to-end", "QA", "Pending", "—"], ["Admin approval gate enforced", "QA", "Pending", "—"], ["Blind screening verified", "QA — photo absent in preview/shortlist", "Pending", "—"], ["Audit immutability verified", "QA — delete attempts rejected", "Pending", "—"], ["Justification engine output review", "QA — no age/DOB references", "Pending", "—"]]},
        "6. Attorney Notes": {"narrative": "Held in disclaimer master.", "rows": [["Reference", "Disclaimer Master v3.7 + ILLM-09-014 Labour Act", "Legal", "Reference"]]},
        "7. Assumptions and Constraints": {"narrative": "Constitution Article 10 grounds list mirrored in D-04.", "rows": [["Constitution-aligned grounds", "Sex, race, colour, ethnic origin, religion, creed, social or economic status", "Legal", "Aligned"]]},
        "8. Approval": {"narrative": "Attorney sign-off pre-launch.", "rows": [["Attorney", "Sign-off", "External", "Pending"]]},
    },
}

BATCH3["09_Compliance_Legal/Audit_Trail_Documentation/ILLM-09-018_Audit_Trail_Documentation_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 spec §15.3 — immutable audit trail with 7-year retention.",
    "sections": {
        "2. Purpose": {"narrative": "Document the platform's audit-trail design, retention, and integrity controls.", "rows": [["Purpose", "Audit trail documentation", "Compliance", "Active"]]},
        "3. Legal Framework": {"narrative": "Audit-trail expectations from Labour Act, ETA, Income Tax Act, ISO 27001 best practice.", "rows": [["Income Tax Act", "Financial records 7 years", "Compliance", "Required"], ["Compliance evidence", "Sensitive-filter justifications, admin overrides, refunds", "Compliance", "Required"], ["ISO 27001 A.12.4", "Logging and monitoring", "Architect", "Aligned"]]},
        "4. Compliance Requirements": {"narrative": "Required audit events and retention.", "rows": [["AT-1 Sensitive filter use", "filter type, value, employer id, justification text, timestamp — 7y retention", "Architect", "Implemented"], ["AT-2 Candidate unlock", "employer id, candidate id, request id, timestamp, IP — 7y", "Architect", "Implemented"], ["AT-3 Report generation", "request id, employer id, generation timestamp, admin approver — 7y", "Architect", "Implemented"], ["AT-4 Payment processed", "Full payment record + gateway reference — 7y", "Architect", "Implemented"], ["AT-5 Internal portal events", "Generation, submission, close, processing — 7y", "Architect", "Implemented"], ["AT-6 Student lifecycle", "Verification, graduation prompt, upgrade — 7y", "Architect", "Implemented"], ["AT-7 Admin override actions", "Action type, entity, admin id, timestamp — 7y", "Architect", "Implemented"], ["AT-8 Founder grants and revocations", "Full record — permanent retention", "Architect", "Implemented"], ["AT-9 Auto-Application audit ledger", "Per (request, candidate) — 7y", "Architect", "Implemented (v3.7)"], ["AT-10 AI Assistant escalations", "Conversation transcript + context — 7y for escalated; 90d non-escalated", "Architect", "Implemented"]]},
        "5. Compliance Checklist": {"narrative": "Integrity tests.", "rows": [["Append-only enforced", "No UI or API path can delete audit_logs", "QA", "Pending"], ["Retention purge tested", "Non-audit records purged per D-12; audit records retained", "QA", "Pending"], ["Backup includes audit", "Audit included in daily backups + cross-region replication", "DevOps", "Verified"], ["Tamper detection", "Optional Phase 8 — append-only hash chain for forensic integrity", "Architect", "Future"]]},
        "6. Attorney Notes": {"narrative": "Audit-trail compliance posture is for attorney review.", "rows": [["Review", "Confirm 7y retention meets Income Tax Act and Labour Act expectations", "Attorney", "Open"]]},
        "7. Assumptions and Constraints": {"narrative": "Retention windows assume current Namibian requirements.", "rows": [["Retention", "7 years for compliance/audit records", "Compliance", "Confirmed"]]},
        "8. Approval": {"narrative": "Attorney sign-off pre-launch.", "rows": [["Attorney", "Sign-off", "External", "Pending"]]},
    },
}

BATCH3["09_Compliance_Legal/Attorney_Review/ILLM-09-013_Attorney_Review_v1_0.docx"] = {
    "v2_change_description": "Attorney review log for the disclaimer pack and compliance documents.",
    "sections": {
        "2. Purpose": {"narrative": "Track attorney review status for all 12 disclaimers plus compliance documents.", "rows": [["Purpose", "Attorney review tracking", "Legal", "Active"]]},
        "3. Legal Framework": {"narrative": "Reviewed by qualified Namibian attorney before any disclaimer goes live.", "rows": [["Reviewer", "TBD — qualified Namibian attorney", "Legal", "Engagement pending"]]},
        "4. Compliance Requirements": {"narrative": "All client-facing disclaimers must be attorney-approved before first live use.", "rows": [["Pre-launch attorney sign-off", "Mandatory for D-01 through D-12", "Legal", "Mandatory"]]},
        "5. Compliance Checklist": {"narrative": "Per-disclaimer review status.", "rows": [
            ["D-01 Platform Technology", "Drafted — pending review", "Attorney", "Open"],
            ["D-02 Report Disclaimer (v3.7 wording)", "Drafted — pending review", "Attorney", "Open"],
            ["D-03 Employer Compliance Declaration", "Drafted — pending review", "Attorney", "Open"],
            ["D-04 Sensitive Filter Warning", "Drafted — pending review", "Attorney", "Open"],
            ["D-05 Candidate Data Consent (with Auto-Application clause)", "Drafted — pending review", "Attorney", "Open"],
            ["D-06 Student Registration Consent", "Drafted — pending review", "Attorney", "Open"],
            ["D-07 Internal Portal Notice", "Drafted — pending review", "Attorney", "Open"],
            ["D-08 CV Processing Notice", "Drafted — pending review", "Attorney", "Open"],
            ["D-09 Candidate Unlock Notice", "Drafted — pending review", "Attorney", "Open"],
            ["D-10 Email Footer", "Drafted — pending review", "Attorney", "Open"],
            ["D-11 Website General", "Drafted — pending review", "Attorney", "Open"],
            ["D-12 Data Retention", "Drafted — pending review", "Attorney", "Open"],
            ["ILLM-09-014 Labour Act compliance", "Drafted — pending review", "Attorney", "Open"],
            ["ILLM-09-015 Data Protection", "Drafted — pending review", "Attorney", "Open"],
            ["ILLM-09-016 Anti-Discrimination", "Drafted — pending review", "Attorney", "Open"],
            ["ILLM-09-017 PCI-DSS Compliance", "Drafted — pending review", "Attorney", "Open"],
            ["ILLM-09-018 Audit Trail", "Drafted — pending review", "Attorney", "Open"],
        ]},
        "6. Attorney Notes": {"narrative": "Notes captured per disclaimer in the disclaimer master file.", "rows": [["Source", "Illumin360_Disclaimer_Document_v3.7 master", "Legal", "Reference"]]},
        "7. Assumptions and Constraints": {"narrative": "Engagement of qualified Namibian attorney required.", "rows": [["Attorney engagement", "Procure ahead of pre-launch window", "Legal", "Required"]]},
        "8. Approval": {"narrative": "Closure of attorney review log requires sign-off on all 17 items.", "rows": [["Final sign-off", "All disclaimers + compliance docs approved", "Attorney", "Pending"]]},
    },
}

# ───────────────── Folder 13 — Governance ────────────────────────────────────

GOVERNANCE_DOCS = {
    "13_Project_Governance/Meeting_Minutes/Steering_Committee/ILLM-13-001_Steering_Committee_Minutes_Template_v1_0.docx": ("Steering Committee Minutes Template", "Template for quarterly Steering Committee meetings. Records attendees, agenda, decisions, action items, next meeting date.", "Quarterly"),
    "13_Project_Governance/Meeting_Minutes/Project_Team/ILLM-13-002_Project_Team_Minutes_Template_v1_0.docx": ("Project Team Minutes Template", "Template for project team check-ins. Records attendees, status, blockers, decisions, action items.", "Weekly"),
    "13_Project_Governance/Meeting_Minutes/Client_Reviews/ILLM-13-003_Client_Review_Minutes_Template_v1_0.docx": ("Client Review Minutes Template", "Template for client review meetings (Founding Partners, university partners). Records attendees, feedback, action items.", "As needed"),
    "13_Project_Governance/Status_Reports/ILLM-13-004_Status_Report_Template_v1_0.docx": ("Status Report Template", "Weekly status report template — progress, risks, blockers, next-week plan.", "Weekly"),
    "13_Project_Governance/Change_Control_Log/ILLM-13-005_Change_Control_Log_v1_0.docx": ("Change Control Log", "Live register of change requests against the v3.7 spec and scope baseline. Tracks request, decision, impact, owner.", "Continuous"),
    "13_Project_Governance/Issue_Log/ILLM-13-006_Issue_Log_v1_0.docx": ("Issue Log", "Live register of project issues — open and closed. Tracks raise date, owner, status, resolution.", "Continuous"),
    "13_Project_Governance/Risk_Log/ILLM-13-007_Risk_Log_v1_0.docx": ("Risk Log", "Live risk register superseding the Initial Risk Register (ILLM-01-005). Reviewed weekly during build, monthly post-launch.", "Continuous"),
    "13_Project_Governance/Decision_Log/ILLM-13-008_Decision_Log_v1_0.docx": ("Decision Log", "Architecture and project decisions with rationale. Each decision becomes an ADR. Linked from the architecture document (ILLM-03-001).", "Per decision"),
    "13_Project_Governance/Sign_Off_Register/ILLM-13-009_Sign_Off_Register_v1_0.docx": ("Sign-Off Register", "Master register of all sign-offs across the SDLC. Each entry links to the signing document (ILLM-01-006, ILLM-02-007, ILLM-03-010, etc.).", "Per sign-off"),
}

for path, (title, purpose, cadence) in GOVERNANCE_DOCS.items():
    BATCH3[path] = {
        "v2_change_description": f"Populated {title}. Template ready for use.",
        "sections": {
            "2. Purpose": {"narrative": purpose, "rows": [["Purpose", purpose, "PM", "Active"], ["Cadence", cadence, "PM", "—"]]},
            "3. Decision Entry": {"narrative": "Each entry follows the structure: date, decision, rationale, alternatives considered, decided by, status.", "rows": [["Field", "Description", "—", "—"], ["Date", "ISO 8601", "—", "Required"], ["Decision summary", "One-line statement", "—", "Required"], ["Rationale", "Why this decision was made", "—", "Required"], ["Alternatives considered", "Other options and why rejected", "—", "Required"], ["Decided by", "Signing authority", "—", "Required"], ["Status", "Open / Closed / Superseded", "—", "Required"]]} if "Decision_Log" in path else
            {"3. Stakeholder Identification": {"narrative": "—", "rows": [["—", "—", "—", "—"]]}}.get("3. Stakeholder Identification", {"narrative": "Section content follows the template structure.", "rows": [["Section", "Captures cadence-specific records", "PM", "Active"]]}),
            "4. Assumptions and Constraints": {"narrative": "Template assumes standard SDLC governance.", "rows": [["Assumption", "Template applies through full SDLC", "PM", "Confirmed"]]},
            "5. Review and Approval": {"narrative": f"Reviewed by PM and Sponsor on cadence: {cadence}.", "rows": [["PM", "Continuous", "PM", "—"], ["Sponsor", cadence, "—", "—"]]},
        },
    }

# Special handling for docs with non-standard section names
BATCH3["13_Project_Governance/Change_Control_Log/ILLM-13-005_Change_Control_Log_v1_0.docx"]["sections"] = {
    "2. Purpose": {"narrative": "Live register of change requests against the v3.7 corrected spec baseline.", "rows": [["Purpose", "Change request tracking", "PM", "Active"]]},
    "3. Change Request Entry": {"narrative": "Each change request has the following structure.", "rows": [["Field", "Description", "—", "—"], ["CR-ID", "Sequential identifier", "—", "Required"], ["Date raised", "ISO 8601", "—", "Required"], ["Requested by", "Name and role", "—", "Required"], ["Change summary", "One-line", "—", "Required"], ["Rationale", "Why required", "—", "Required"], ["Impact", "Scope, schedule, budget, risk", "—", "Required"], ["Decision", "Approved / Rejected / Deferred", "—", "Required"], ["Decided by", "Sponsor or change board", "—", "Required"]]},
    "4. Assumptions and Constraints": {"narrative": "v3.7 spec is the baseline; changes must be reflected in spec amendments.", "rows": [["Baseline", "v3.7 corrected spec", "Architect", "Authoritative"]]},
    "5. Review and Approval": {"narrative": "Reviewed continuously.", "rows": [["Sponsor", "Approves material changes", "—", "—"], ["Architect", "Assesses impact", "—", "—"]]},
}

BATCH3["13_Project_Governance/Sign_Off_Register/ILLM-13-009_Sign_Off_Register_v1_0.docx"]["sections"] = {
    "2. Purpose": {"narrative": "Master register of every signed SDLC artefact across all 14 folders.", "rows": [["Purpose", "Sign-off master register", "PM", "Active"]]},
    "3. Sign-Off Scope": {"narrative": "Every signed artefact appears here keyed by ILLM-ID and folder.", "rows": [
        ["ILLM-01-006 Initiation Sign-Off", "Phase 01 closure", "Sponsor", "Pending"],
        ["ILLM-02-007 Requirements Sign-Off", "Phase 02 closure", "Sponsor", "Pending"],
        ["ILLM-03-010 Design Review Sign-Off", "Phase 03 closure", "Sponsor", "Pending"],
        ["ILLM-06-009 Increment Sign-Off (per phase)", "Each incremental phase closure", "Sponsor", "Per phase"],
        ["ILLM-08-016 UAT Sign-Off", "User acceptance testing closure", "Sponsor", "Pending"],
        ["ILLM-10-008 Environment Sign-Off", "Production go-live", "Sponsor", "Pending"],
    ]},
    "4. Sign-Off Record": {"narrative": "Detailed record per signed item; format: date, signer, item, condition flags.", "rows": [["Record format", "Date | Signer | ILLM-ID | Conditions", "PM", "Standard"]]},
    "5. Assumptions and Constraints": {"narrative": "Sign-offs are permanent — no rescission without re-sign-off on a successor document.", "rows": [["Permanence", "Sign-offs immutable once recorded", "—", "Standard"]]},
    "6. Review and Approval": {"narrative": "Reviewed quarterly.", "rows": [["Quarterly review", "Confirm completeness", "PM", "Scheduled"]]},
}

# ───────────────── Folder 14 — Configuration & Asset Management ──────────────

BATCH3["14_Configuration_Asset_Management/Version_Control_Policy/ILLM-14-001_Version_Control_Policy_v1_0.docx"] = {
    "v2_change_description": "Populated — Git-based version control policy with branching strategy.",
    "sections": {
        "2. Purpose": {"narrative": "Define source-code version control policy for the platform.", "rows": [["Purpose", "Version control policy", "DevOps", "Active"]]},
        "3. Configuration Items": {"narrative": "Repositories under version control.", "rows": [
            ["Application code monorepo or per-service", "TBD — recommend monorepo for related services", "DevOps", "Open"],
            ["Database migrations", "/migrations folder; sequential numbered files", "DBA", "Standard"],
            ["Infrastructure-as-code", "K8s manifests, Helm charts, Argo CD apps", "DevOps", "Standard"],
            ["Documentation source", "SDLC documents and OpenAPI specs", "Architect", "Standard"],
        ]},
        "4. Assumptions and Constraints": {"narrative": "Git as VCS; GitHub/GitLab/Gitea TBD.", "rows": [["VCS", "Git", "DevOps", "Standard"], ["Hosting", "GitHub or self-hosted Gitea/Forgejo — open", "DevOps", "Open"], ["Branching", "Trunk-based with short-lived feature branches", "DevOps", "Standard"], ["PR review", "Mandatory before merge to main", "DevOps", "Standard"], ["Tagging", "Semantic versioning; per-service or per-release", "DevOps", "Standard"]]},
        "5. Review and Approval": {"narrative": "Policy reviewed annually.", "rows": [["Annual review", "DevOps + Architect", "—", "Annual"]]},
    },
}

BATCH3["14_Configuration_Asset_Management/Configuration_Items_Register/ILLM-14-002_Configuration_Items_Register_v1_0.docx"] = {
    "v2_change_description": "Populated — register of all configuration items per spec §36 OSS stack.",
    "sections": {
        "2. Purpose": {"narrative": "Master register of platform configuration items (CIs).", "rows": [["Purpose", "CI register", "DevOps", "Active"]]},
        "3. Configuration Items": {"narrative": "CIs grouped by category.", "rows": [
            ["CI-IDP-1 Keycloak", "IAM authority — version pinned", "DevOps", "Tracked"],
            ["CI-QUE-1 RabbitMQ", "Message broker", "DevOps", "Tracked"],
            ["CI-DB-1 PostgreSQL 15+", "Primary database", "DBA", "Tracked"],
            ["CI-DB-2 pgvector", "Vector search extension", "DBA", "Tracked"],
            ["CI-DB-3 Redis", "Cache and session", "DevOps", "Tracked"],
            ["CI-STG-1 MinIO", "Object storage", "DevOps", "Tracked"],
            ["CI-OBS-1 Grafana OSS", "Visualisation", "DevOps", "Tracked"],
            ["CI-OBS-2 Prometheus", "Metrics", "DevOps", "Tracked"],
            ["CI-OBS-3 Loki", "Logs", "DevOps", "Tracked"],
            ["CI-OBS-4 Tempo", "Traces", "DevOps", "Tracked"],
            ["CI-OBS-5 OpenTelemetry", "Instrumentation", "DevOps", "Tracked"],
            ["CI-WF-1 Temporal", "Workflow orchestration", "DevOps", "Tracked"],
            ["CI-GW-1 Kong / Traefik", "API gateway — selection open", "DevOps", "Open"],
            ["CI-RP-1 NGINX", "Reverse proxy", "DevOps", "Tracked"],
            ["CI-SEC-1 ClamAV", "Virus scan", "DevOps", "Tracked"],
            ["CI-SEC-2 HashiCorp Vault", "Secrets", "DevOps", "Tracked"],
            ["CI-DOC-1 Apache Tika", "Doc parsing", "DevOps", "Tracked"],
            ["CI-OCR-1 Tesseract", "OCR fallback", "DevOps", "Tracked"],
            ["CI-AI-1 Anthropic API key", "Claude Sonnet 4.6", "DevOps (Vault)", "Tracked"],
            ["CI-AI-2 Google Cloud Vision key", "OCR primary", "DevOps (Vault)", "Tracked"],
            ["CI-PAY-1 Payment gateway", "TBD selection", "DevOps", "Open"],
            ["CI-EMAIL-1 Email provider", "TBD selection", "DevOps", "Open"],
            ["CI-CRT-1 Kubernetes / K3s", "Orchestration", "DevOps", "Open"],
            ["CI-CD-1 Argo CD", "GitOps deployment", "DevOps", "Tracked"],
        ]},
        "4. Assumptions and Constraints": {"narrative": "All CIs version-pinned in production; upgrades per ILLM-03-002 Section 6.", "rows": [["Version pinning", "Mandatory for production", "DevOps", "Standard"]]},
        "5. Review and Approval": {"narrative": "Reviewed quarterly.", "rows": [["Quarterly review", "DevOps", "—", "Scheduled"]]},
    },
}

BATCH3["14_Configuration_Asset_Management/Licence_Register/ILLM-14-003_Licence_Register_v1_0.docx"] = {
    "v2_change_description": "Populated — software licence register.",
    "sections": {
        "2. Purpose": {"narrative": "Track licences for all software components.", "rows": [["Purpose", "Licence tracking", "Compliance", "Active"]]},
        "3. Configuration Items": {"narrative": "Per-component licence summary.", "rows": [
            ["Keycloak", "Apache 2.0", "No fee", "OK"],
            ["RabbitMQ", "Mozilla Public Licence 2.0", "No fee", "OK"],
            ["PostgreSQL", "PostgreSQL Licence (BSD-style)", "No fee", "OK"],
            ["Grafana OSS, Prometheus, Loki, Tempo, OpenTelemetry, k6", "AGPL-3.0 / Apache 2.0", "No fee", "OK"],
            ["MinIO", "AGPL-3.0", "No fee; commercial licence if SaaS-with-modifications", "Verify"],
            ["Redis", "BSD-3 / RSAL — confirm version", "No fee for current usage", "Verify"],
            ["HashiCorp Vault OSS", "BUSL", "Commercial use permitted with restrictions", "Verify"],
            ["Temporal", "MIT", "No fee", "OK"],
            ["ClamAV", "GPL v2", "No fee", "OK"],
            ["Apache Tika, Tesseract", "Apache 2.0 / Apache 2.0", "No fee", "OK"],
            ["Anthropic Claude Sonnet 4.6", "Pay-per-use API — no licence fee per se", "Usage-based", "Active"],
            ["Google Cloud Vision", "Pay-per-use API", "Usage-based + free tier", "Active"],
        ]},
        "4. Assumptions and Constraints": {"narrative": "Licence reviewed before each major version upgrade.", "rows": [["Upgrade review", "Verify licence terms on each upgrade", "Compliance", "Standard"]]},
        "5. Review and Approval": {"narrative": "Reviewed annually.", "rows": [["Annual review", "Compliance + DevOps", "—", "Annual"]]},
    },
}

BATCH3["14_Configuration_Asset_Management/Infrastructure_Inventory/ILLM-14-004_Infrastructure_Inventory_v1_0.docx"] = {
    "v2_change_description": "Populated — infrastructure inventory placeholder; actual hosts/instances populated by DevOps.",
    "sections": {
        "2. Purpose": {"narrative": "Inventory of all infrastructure hosts, instances, and managed services.", "rows": [["Purpose", "Infra inventory", "DevOps", "Active"]]},
        "3. Configuration Items": {"narrative": "Inventory by environment (dev / staging / production).", "rows": [
            ["Kubernetes cluster — production", "TBD — node specs, region, K8s version", "DevOps", "To populate"],
            ["Kubernetes cluster — staging", "TBD", "DevOps", "To populate"],
            ["Kubernetes cluster — dev", "TBD", "DevOps", "To populate"],
            ["PostgreSQL — production", "Managed or self-hosted; HA configuration", "DBA", "To populate"],
            ["PostgreSQL — staging", "TBD", "DBA", "To populate"],
            ["Object storage — production (MinIO)", "Cluster size, replication zones", "DevOps", "To populate"],
            ["Keycloak — production", "HA cluster; realms configured", "DevOps", "To populate"],
            ["RabbitMQ — production", "HA cluster", "DevOps", "To populate"],
            ["Redis — production", "HA setup", "DevOps", "To populate"],
            ["Observability stack", "Grafana, Prometheus, Loki, Tempo deployment", "DevOps", "To populate"],
            ["CI/CD runners", "Build pool sizing", "DevOps", "To populate"],
            ["Domain and TLS certificates", "Per environment; cert-manager automation", "DevOps", "To populate"],
        ]},
        "4. Assumptions and Constraints": {"narrative": "Inventory must reflect production reality and be kept current.", "rows": [["Currency", "Updated on every infrastructure change", "DevOps", "Mandatory"]]},
        "5. Review and Approval": {"narrative": "Reviewed quarterly.", "rows": [["Quarterly review", "DevOps", "—", "Scheduled"]]},
    },
}
