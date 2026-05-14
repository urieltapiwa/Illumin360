"""Fixes for batch 3 — corrected section headings for disclaimers D-03..D-12 and governance/config templates."""

FIXES3 = {}

# Disclaimer common section pattern: 2.Purpose, 3.Legal Framework, 4.Disclaimer Text, 5.Implementation Location, 6.Attorney Review Status, 7.Attorney Notes
# Per-disclaimer details
_D = {
    "D-03": {
        "path": "09_Compliance_Legal/Disclaimers/D03_Employer_Compliance_Declaration/ILLM-09-003_D03_Employer_Compliance_Declaration_v1_0.docx",
        "title": "Employer Compliance Declaration",
        "purpose": "Require the Business to confirm fair, non-discriminatory selection criteria before each request is submitted.",
        "location": "Recruitment request submission — Step 4 (Review & Submit). Checkbox must be ticked; cannot be pre-ticked.",
        "framework": "Constitution Article 10 (non-discrimination); Labour Act 11/2007.",
    },
    "D-05": {
        "path": "09_Compliance_Legal/Disclaimers/D05_Candidate_Data_Consent/ILLM-09-005_D05_Candidate_Data_Consent_v1_0.docx",
        "title": "Candidate Data Consent Statement",
        "purpose": "Capture explicit consent for data collection, profile visibility, automated processing, and the Auto-Application standing-application principle.",
        "location": "Professional registration — final step. Checkbox must be ticked; registration blocked until ticked.",
        "framework": "Electronic Transactions Act 4/2019; GDPR-equivalent best practice.",
    },
    "D-06": {
        "path": "09_Compliance_Legal/Disclaimers/D06_Student_Registration_Consent/ILLM-09-006_D06_Student_Registration_Consent_v1_0.docx",
        "title": "Student Registration Consent",
        "purpose": "Capture additional consent specific to Student CSR — verification, institution data sharing, graduate upgrade.",
        "location": "Student registration — second checkbox after D-05; both required.",
        "framework": "Electronic Transactions Act 4/2019; institutional data sharing under partnership agreements.",
    },
    "D-07": {
        "path": "09_Compliance_Legal/Disclaimers/D07_Internal_Portal_Notice/ILLM-09-007_D07_Internal_Portal_Notice_v1_0.docx",
        "title": "Internal Recruitment Portal Applicant Notice",
        "purpose": "Notify internal applicants of automated processing and limited use of data for the specific vacancy only.",
        "location": "Public-facing internal recruitment portal page — above the application form; persistent.",
        "framework": "Electronic Transactions Act 4/2019; automated decision-making transparency.",
    },
    "D-08": {
        "path": "09_Compliance_Legal/Disclaimers/D08_CV_Processing_Notice/ILLM-09-008_D08_CV_Processing_Notice_v1_0.docx",
        "title": "CV Processing Notice",
        "purpose": "Acknowledge that uploaded CVs are stored encrypted and processed automatically without human review in normal operation.",
        "location": "CV upload confirmation screen; modal or inline notice after upload.",
        "framework": "Electronic Transactions Act 4/2019; data minimisation principles.",
    },
    "D-09": {
        "path": "09_Compliance_Legal/Disclaimers/D09_Candidate_Unlock_Notice/ILLM-09-009_D09_Candidate_Unlock_Notice_v1_0.docx",
        "title": "Candidate Profile Unlock Notice",
        "purpose": "Confirm permitted use, data protection, no-guarantee, and non-refundable nature of candidate unlock fees.",
        "location": "Candidate unlock payment modal — before payment initiation.",
        "framework": "Data protection responsibilities transferred to employer post-unlock.",
    },
    "D-10": {
        "path": "09_Compliance_Legal/Disclaimers/D10_Email_Footer/ILLM-09-010_D10_Email_Footer_Disclaimer_v1_0.docx",
        "title": "Platform Email Footer Disclaimer",
        "purpose": "Reiterate platform-not-agency framing and confidentiality on every system-generated email.",
        "location": "Auto-appended to footer of every system-generated email.",
        "framework": "Electronic Communications and Transactions Act; anti-spam.",
    },
    "D-11": {
        "path": "09_Compliance_Legal/Disclaimers/D11_Website_General/ILLM-09-011_D11_Website_General_Disclaimer_v1_0.docx",
        "title": "Website General Disclaimer",
        "purpose": "Public website general disclaimer reiterating platform-not-agency framing and limitations of liability.",
        "location": "Website footer on every page; About page; Terms of Service page.",
        "framework": "Labour Act 11/2007; consumer protection; contract law.",
    },
    "D-12": {
        "path": "09_Compliance_Legal/Disclaimers/D12_Data_Retention/ILLM-09-012_D12_Data_Retention_Notice_v1_0.docx",
        "title": "Data Retention Notice",
        "purpose": "Explain personal data retention periods per data category.",
        "location": "Account settings page; Privacy Policy page.",
        "framework": "Income Tax Act (7-year financial records); ETA 4/2019; data minimisation.",
    },
}

for code, info in _D.items():
    FIXES3[info["path"]] = {
        "v2_change_description": f"Populated from v3.7 disclaimer master (corrected). {info['title']} ({code}). Section 31 branding policy compliant.",
        "sections": {
            "2. Purpose": {
                "narrative": info["purpose"],
                "rows": [
                    ["Purpose", info["purpose"], "Legal", "Required"],
                    ["Audience", info["location"][:80], "Marketing", "—"],
                    ["Branding", "Section 31 — Illumin360 only; no third-party-vendor mentions in client-facing content", "Marketing", "Mandatory"],
                ],
            },
            "3. Legal Framework": {
                "narrative": info["framework"],
                "rows": [["Framework", info["framework"], "Legal", "Reference"]],
            },
            "4. Disclaimer Text": {
                "narrative": "Verbatim text per v3.7 disclaimer master. Reproduced exactly in the platform's UI/template/email-footer per location.",
                "rows": [
                    [f"{code} heading", info["title"], "Marketing", "Approved"],
                    ["Source", "Illumin360 Disclaimer Master v3.7 (corrected)", "Legal", "Authoritative"],
                    ["Status", "Drafted — pending attorney review", "Legal", "Pending"],
                ],
            },
            "5. Implementation Location": {
                "narrative": info["location"],
                "rows": [[f"{code} location", info["location"], "Architect", "Spec'd"]],
            },
            "6. Attorney Review Status": {
                "narrative": "Requires Namibian attorney sign-off before go-live.",
                "rows": [
                    ["Status", "Drafted — pending attorney review", "Legal", "Pending"],
                    ["Reviewer", "TBD — qualified Namibian attorney", "Legal", "Pending"],
                    ["Target sign-off", "Before first live use", "Sponsor", "Required"],
                ],
            },
            "7. Attorney Notes": {
                "narrative": "Review questions held in the disclaimer master file.",
                "rows": [
                    [f"{code} review Q1", "Confirm wording is enforceable under Namibian law", "Attorney", "Open"],
                    [f"{code} review Q2", "Confirm Section 31 branding-compatible wording is legally sufficient", "Attorney", "Open"],
                    [f"{code} review Q3", "Confirm statutory cross-references are current", "Attorney", "Open"],
                ],
            },
        },
    }

# D-04 has an extra section (Anti-Discrimination Controls between Legal Framework and Disclaimer Text)
FIXES3["09_Compliance_Legal/Disclaimers/D04_Sensitive_Filter_Warning/ILLM-09-004_D04_Sensitive_Filter_Warning_v1_0.docx"] = {
    "v2_change_description": "Populated from v3.7 disclaimer master. D-04 Sensitive Filter Warning with anti-discrimination control flow.",
    "sections": {
        "2. Purpose": {
            "narrative": "Display a legal warning when gender or age is selected as a filter; require ≥50-word justification and admin approval before report unlock.",
            "rows": [
                ["Purpose", "Anti-discrimination warning and justification gate", "Legal", "Required"],
                ["Audience", "Business creating recruitment request with sensitive filter", "Marketing", "—"],
                ["Branding", "Section 31 — Illumin360 only", "Marketing", "Mandatory"],
            ],
        },
        "3. Legal Framework": {
            "narrative": "Constitution Article 10 anti-discrimination; Labour Act 11/2007 EEO provisions; Affirmative Action (Employment) Act 29/1998 lawful-justification grounds.",
            "rows": [
                ["Constitution Article 10", "Equality and non-discrimination", "Legal", "Critical"],
                ["Labour Act 11/2007", "EEO provisions", "Legal", "Critical"],
                ["Affirmative Action Act 29/1998", "Lawful affirmative-action grounds", "Legal", "Critical"],
            ],
        },
        "4. Anti-Discrimination Controls": {
            "narrative": "Platform-side controls enforced when D-04 fires.",
            "rows": [
                ["Trigger", "Gender or age filter selected on a recruitment request", "Architect", "Auto"],
                ["Mandatory justification", "≥50 words; stored immutably in compliance_justifications", "Architect", "Implemented"],
                ["Declaration D-03", "Must be confirmed before submission", "Architect", "Implemented"],
                ["Admin alert", "Immediate notification to admin queue", "Architect", "Implemented"],
                ["Admin approval", "Required before Business can unlock the report", "Architect", "Implemented"],
                ["Audit", "Filter type, value, employer ID, justification, timestamp — 7y retention", "Architect", "Implemented"],
            ],
        },
        "5. Disclaimer Text": {
            "narrative": "Verbatim text per v3.7 disclaimer master.",
            "rows": [
                ["D-04 heading", "Sensitive Filter Legal Warning", "Marketing", "Approved"],
                ["Source", "Illumin360 Disclaimer Master v3.7", "Legal", "Authoritative"],
                ["Status", "Drafted — pending attorney review", "Legal", "Pending"],
            ],
        },
        "6. Implementation Location": {
            "narrative": "Recruitment request form — Filters section. Auto-displayed when gender or age filter is selected; cannot be dismissed.",
            "rows": [["D-04 location", "Filters section of request form; auto-triggered on sensitive filter", "Architect", "Spec'd"]],
        },
        "7. Attorney Review Status": {
            "narrative": "Highest-stakes disclaimer — confirms lawful justification grounds list.",
            "rows": [
                ["Status", "Drafted — pending attorney review", "Legal", "Pending"],
                ["Specific review Q", "Confirm grounds list (bona fide occupational requirement; affirmative action plan; expressly permitted) aligns with current Namibian case law", "Attorney", "Open"],
            ],
        },
        "8. Attorney Notes": {
            "narrative": "Notes held in the disclaimer master file.",
            "rows": [["D-04 review Q1", "Confirm 50-word minimum is appropriate; advise on alternative threshold", "Attorney", "Open"]],
        },
    },
}

# ───────────────── Folder 13 Governance — corrected section keys ────────────

FIXES3["13_Project_Governance/Meeting_Minutes/Steering_Committee/ILLM-13-001_Steering_Committee_Minutes_Template_v1_0.docx"] = {
    "v2_change_description": "Steering Committee Minutes Template — sections aligned to template structure.",
    "sections": {
        "2. Purpose": {"narrative": "Template for quarterly Steering Committee meetings.", "rows": [["Purpose", "Quarterly governance review", "PM", "Active"], ["Attendees baseline", "Sponsor, Architect, Marketing Lead, Finance Lead", "—", "—"]]},
        "3. Meeting Agenda Template": {"narrative": "Standard agenda items.", "rows": [["1. Project status by phase", "Architect", "10 min", "—"], ["2. Risk register review", "PM", "10 min", "—"], ["3. Financial review", "Finance", "10 min", "—"], ["4. Strategic decisions", "Sponsor", "20 min", "—"], ["5. AOB and next meeting", "PM", "10 min", "—"]]},
        "4. Meeting Minutes Template": {"narrative": "Per-meeting record.", "rows": [["Date / Time / Location", "ISO 8601", "—", "Required"], ["Attendees", "List with roles", "—", "Required"], ["Apologies", "Absent members", "—", "—"], ["Decisions", "Linked to Decision Log ILLM-13-008", "—", "Required"], ["Risks raised", "Linked to Risk Log ILLM-13-007", "—", "—"], ["Issues raised", "Linked to Issue Log ILLM-13-006", "—", "—"]]},
        "5. Action Items": {"narrative": "Tracked across meetings.", "rows": [["AI-#", "Sequential", "—", "Required"], ["Description", "—", "—", "Required"], ["Owner", "Named", "—", "Required"], ["Due date", "—", "—", "Required"], ["Status", "Open / Closed / Carried over", "—", "Required"]]},
        "6. Review and Approval": {"narrative": "Minutes approved at next meeting.", "rows": [["Approval", "Approve at subsequent meeting", "Sponsor", "Standard"]]},
    },
}

FIXES3["13_Project_Governance/Meeting_Minutes/Project_Team/ILLM-13-002_Project_Team_Minutes_Template_v1_0.docx"] = {
    "v2_change_description": "Project Team Minutes Template — weekly check-in.",
    "sections": {
        "2. Purpose": {"narrative": "Template for weekly project team check-ins.", "rows": [["Purpose", "Weekly status sync", "PM", "Active"]]},
        "3. Meeting Agenda Template": {"narrative": "Standard agenda.", "rows": [["1. Status by service / phase", "Architect", "10 min", "—"], ["2. Blockers", "All", "10 min", "—"], ["3. Action items review", "PM", "10 min", "—"], ["4. Next week plan", "Architect", "10 min", "—"]]},
        "4. Meeting Minutes Template": {"narrative": "Per-meeting record.", "rows": [["Date / Time / Location", "ISO 8601", "—", "Required"], ["Attendees", "Project team", "—", "Required"], ["Status summary", "Per-service or per-phase status", "—", "Required"], ["Blockers", "Listed and owned", "—", "Required"], ["Decisions", "Linked", "—", "—"]]},
        "5. Action Items": {"narrative": "Carried into status reports.", "rows": [["AI-#", "Sequential", "—", "Required"]]},
        "6. Review and Approval": {"narrative": "Approved at next meeting.", "rows": [["Approval", "Approve next meeting", "PM", "Standard"]]},
    },
}

FIXES3["13_Project_Governance/Meeting_Minutes/Client_Reviews/ILLM-13-003_Client_Review_Minutes_Template_v1_0.docx"] = {
    "v2_change_description": "Client Review Minutes Template — for Founding Partner and university-partner reviews.",
    "sections": {
        "2. Purpose": {"narrative": "Template for client review meetings.", "rows": [["Purpose", "Client review record", "Marketing", "Active"]]},
        "3. Meeting Agenda Template": {"narrative": "Agenda.", "rows": [["1. Welcome and introductions", "Sponsor / Marketing", "5 min", "—"], ["2. Platform update", "Architect", "15 min", "—"], ["3. Client feedback session", "Client", "20 min", "—"], ["4. Action items and next steps", "PM", "10 min", "—"]]},
        "4. Meeting Minutes Template": {"narrative": "Per-meeting record.", "rows": [["Date / Time / Location", "ISO 8601", "—", "Required"], ["Client", "Founding Partner / institution / etc.", "—", "Required"], ["Attendees", "—", "—", "Required"], ["Feedback summary", "Client input", "—", "Required"], ["Decisions", "Linked", "—", "—"]]},
        "5. Action Items": {"narrative": "Per client.", "rows": [["AI-#", "—", "—", "Required"]]},
        "6. Review and Approval": {"narrative": "Minutes shared with client for confirmation.", "rows": [["Client confirmation", "Within 5 business days", "Marketing", "Standard"], ["Internal sign-off", "Sponsor", "—", "—"]]},
    },
}

FIXES3["13_Project_Governance/Status_Reports/ILLM-13-004_Status_Report_Template_v1_0.docx"] = {
    "v2_change_description": "Status Report Template — weekly format.",
    "sections": {
        "2. Purpose": {"narrative": "Weekly status report template.", "rows": [["Purpose", "Weekly status to sponsor", "PM", "Active"]]},
        "3. Status Report": {"narrative": "Status by phase and service.", "rows": [["Reporting period", "Week-ending date", "—", "Required"], ["Overall status", "Green / Amber / Red", "—", "Required"], ["Phase progress", "Per-phase % complete", "Architect", "Required"], ["Service progress", "Per-microservice status", "Architect", "Required"], ["Achievements this week", "Bullet list", "—", "Required"], ["Plan next week", "Bullet list", "—", "Required"]]},
        "4. Risks": {"narrative": "Top-3 risks from Risk Log (ILLM-13-007).", "rows": [["Top risk 1", "Description and mitigation", "—", "—"], ["Top risk 2", "—", "—", "—"], ["Top risk 3", "—", "—", "—"]]},
        "5. Action Items": {"narrative": "Open action items at status time.", "rows": [["Open AIs", "From all meetings", "PM", "Required"]]},
        "6. Review and Approval": {"narrative": "Distributed to Sponsor by EOD Friday.", "rows": [["Distribution", "Sponsor + project team", "PM", "Weekly"]]},
    },
}

FIXES3["13_Project_Governance/Risk_Log/ILLM-13-007_Risk_Log_v1_0.docx"] = {
    "v2_change_description": "Risk Log — live register superseding ILLM-01-005.",
    "sections": {
        "2. Purpose": {"narrative": "Live risk register for the project lifetime.", "rows": [["Purpose", "Active risk tracking", "PM", "Active"]]},
        "3. Risk Management Approach": {"narrative": "Inherited from Risk Management Plan (ILLM-04-007).", "rows": [["Scoring", "Probability × Impact", "PM", "Standard"], ["Cadence build", "Weekly", "PM", "Active"], ["Cadence operate", "Monthly", "PM", "Planned"], ["Escalation", "High×High → Sponsor immediate", "PM", "Standard"]]},
        "4. Risk Entry": {"narrative": "Per-risk record structure.", "rows": [["R-ID", "Sequential", "—", "Required"], ["Category", "Market / Technical / Compliance / Operational / Commercial / Vendor", "—", "Required"], ["Description", "—", "—", "Required"], ["Probability", "L / M / H", "—", "Required"], ["Impact", "L / M / H", "—", "Required"], ["Mitigation", "Owner + plan", "—", "Required"], ["Status", "Open / Mitigated / Closed / Accepted", "—", "Required"]]},
        "5. Assumptions and Constraints": {"narrative": "Risk model assumes Namibian jurisdiction and the v3.7 spec scope.", "rows": [["Scope baseline", "v3.7", "—", "Confirmed"]]},
        "6. Review and Approval": {"narrative": "Reviewed at every Steering Committee.", "rows": [["Steering review", "Quarterly", "Sponsor", "Standard"]]},
    },
}

# Folder 14 fixes
FIXES3["14_Configuration_Asset_Management/Version_Control_Policy/ILLM-14-001_Version_Control_Policy_v1_0.docx"] = {
    "v2_change_description": "Version Control Policy — branching, commit standards, review process.",
    "sections": {
        "2. Purpose": {"narrative": "Source-code version control policy.", "rows": [["Purpose", "VC policy", "DevOps", "Active"]]},
        "3. Version Control Standards": {"narrative": "Git as the VCS; hosting via GitHub or self-hosted Gitea/Forgejo (TBD).", "rows": [["VCS", "Git", "DevOps", "Standard"], ["Hosting", "TBD — GitHub or self-hosted", "DevOps", "Open"], ["Repository structure", "Monorepo recommended for related services; per-service repos acceptable", "DevOps", "Open"], ["Required reviews", "Minimum 1 approval on every PR", "DevOps", "Mandatory"], ["Required CI", "Build + tests + lint + Trivy scan", "DevOps", "Mandatory"]]},
        "4. Branching Strategy": {"narrative": "Trunk-based development with short-lived feature branches.", "rows": [["Default branch", "main", "DevOps", "Standard"], ["Feature branches", "feat/<ticket>; max lifetime 5 days; rebase frequently", "DevOps", "Standard"], ["Release tags", "Semantic versioning; per-service or per-release", "DevOps", "Standard"], ["Hotfix branch", "hotfix/<ticket>; cherry-pick to release tags", "DevOps", "Standard"]]},
        "5. Commit Standards": {"narrative": "Conventional commits.", "rows": [["Format", "<type>(<scope>): <subject>", "Architect", "Standard"], ["Types", "feat, fix, chore, refactor, test, docs, perf, ci", "Architect", "Standard"], ["Scope", "Service name or area", "Architect", "Standard"], ["Subject", "Imperative, < 72 chars", "Architect", "Standard"], ["Body", "Optional; context and rationale", "Architect", "—"]]},
        "6. Review and Approval": {"narrative": "Policy reviewed annually.", "rows": [["Annual review", "DevOps + Architect", "—", "Annual"]]},
    },
}

FIXES3["14_Configuration_Asset_Management/Licence_Register/ILLM-14-003_Licence_Register_v1_0.docx"] = {
    "v2_change_description": "Licence Register — OSS licences plus AI service cost model.",
    "sections": {
        "2. Purpose": {"narrative": "Track licences for all software components and usage costs for paid services.", "rows": [["Purpose", "Licence and cost tracking", "Compliance + Finance", "Active"]]},
        "3. Licence Register": {"narrative": "Per-component licence and licence-fee summary.", "rows": [
            ["Keycloak", "Apache 2.0", "No fee", "OK"],
            ["RabbitMQ", "Mozilla Public Licence 2.0", "No fee", "OK"],
            ["PostgreSQL", "PostgreSQL Licence (BSD-style)", "No fee", "OK"],
            ["Grafana OSS / Prometheus / Loki / Tempo / OpenTelemetry / k6", "AGPL-3.0 / Apache 2.0", "No fee", "OK"],
            ["MinIO", "AGPL-3.0", "Commercial licence if SaaS-with-modifications", "Verify"],
            ["Redis", "BSD-3 / RSAL — confirm", "No fee for current usage", "Verify"],
            ["HashiCorp Vault OSS", "BUSL", "Commercial use permitted with restrictions", "Verify"],
            ["Temporal", "MIT", "No fee", "OK"],
            ["ClamAV", "GPL v2", "No fee", "OK"],
            ["Apache Tika / Tesseract", "Apache 2.0 / Apache 2.0", "No fee", "OK"],
        ]},
        "4. AI Cost Model": {"narrative": "Pay-per-use AI services per spec §29.4.", "rows": [
            ["Anthropic Claude Sonnet 4.6 — CV analysis", "USD 3.00/M input + 15.00/M output (50% batch discount)", "Year 1 ≈ USD 0.75/month", "Active"],
            ["Anthropic Claude Sonnet 4.6 — justification", "Same", "Year 1 ≈ USD 0.50/month", "Active"],
            ["Anthropic Claude Sonnet 4.6 — Assistant", "Same; prompt caching 90% reduction", "Year 1 ≈ USD 3.00/month", "Active"],
            ["Google Cloud Vision OCR", "Free tier first 1,000 pages/month; USD 1.50/1,000 thereafter", "Year 1 USD 0.00", "Active"],
            ["Total Year 1", "—", "≈ NAD 77/month", "Projected"],
            ["Total Year 3", "—", "≈ NAD 1,116/month (0.19% revenue)", "Projected"],
        ]},
        "5. Assumptions and Constraints": {"narrative": "Licence terms verified at each major upgrade.", "rows": [["Upgrade verification", "Verify terms on each major upgrade", "Compliance", "Standard"]]},
        "6. Review and Approval": {"narrative": "Reviewed annually.", "rows": [["Annual review", "Compliance + Finance", "—", "Annual"]]},
    },
}

FIXES3["14_Configuration_Asset_Management/Infrastructure_Inventory/ILLM-14-004_Infrastructure_Inventory_v1_0.docx"] = {
    "v2_change_description": "Infrastructure Inventory — initial structure; populated by DevOps.",
    "sections": {
        "2. Purpose": {"narrative": "Master inventory of infrastructure across environments.", "rows": [["Purpose", "Infra inventory", "DevOps", "Active"]]},
        "3. Infrastructure Inventory": {"narrative": "By environment.", "rows": [
            ["K8s cluster — production", "TBD — node count, version, region", "DevOps", "To populate"],
            ["K8s cluster — staging", "TBD", "DevOps", "To populate"],
            ["K8s cluster — dev", "TBD", "DevOps", "To populate"],
            ["PostgreSQL — production", "HA cluster spec", "DBA", "To populate"],
            ["Keycloak — production", "HA realm configuration", "DevOps", "To populate"],
            ["RabbitMQ — production", "HA cluster", "DevOps", "To populate"],
            ["Redis — production", "HA setup", "DevOps", "To populate"],
            ["MinIO — production", "Cluster size, replication", "DevOps", "To populate"],
            ["LGTM stack", "Grafana + Prometheus + Loki + Tempo deployment", "DevOps", "To populate"],
            ["CI runners", "Capacity", "DevOps", "To populate"],
            ["TLS / DNS", "cert-manager + DNS provider", "DevOps", "To populate"],
        ]},
        "4. AI Cost Model": {"narrative": "Infrastructure-related AI cost tracking — see ILLM-14-003 for full cost model.", "rows": [["Reference", "ILLM-14-003 Section 4", "Finance", "Cross-ref"]]},
        "5. Assumptions and Constraints": {"narrative": "Updated on every infrastructure change.", "rows": [["Currency", "Updated on change", "DevOps", "Mandatory"]]},
        "6. Review and Approval": {"narrative": "Reviewed quarterly.", "rows": [["Quarterly review", "DevOps", "—", "Scheduled"]]},
    },
}

# Audit Trail doc fix
FIXES3["09_Compliance_Legal/Audit_Trail_Documentation/ILLM-09-018_Audit_Trail_Documentation_v1_0.docx"] = {
    "v2_change_description": "Audit Trail Documentation — sections aligned.",
    "sections": {
        "2. Purpose": {"narrative": "Document the platform's audit-trail design, retention, and integrity controls.", "rows": [["Purpose", "Audit trail documentation", "Compliance", "Active"]]},
        "3. Audit Log Structure": {"narrative": "audit_logs table append-only; event taxonomy below.", "rows": [
            ["Event — sensitive_filter_used", "Filter type, value, employer id, justification text, timestamp", "Retention 7y", "—"],
            ["Event — candidate_unlock", "Employer id, candidate id, request id, timestamp, IP", "Retention 7y", "—"],
            ["Event — report_generated", "Request id, employer id, generation ts, admin approver", "Retention 7y", "—"],
            ["Event — payment_processed", "Full payment record + gateway reference", "Retention 7y", "—"],
            ["Event — internal_portal_*", "Generation, submission, close, processing", "Retention 7y", "—"],
            ["Event — student_lifecycle_*", "Verification, graduation prompt, upgrade", "Retention 7y", "—"],
            ["Event — admin_override_*", "Action type, entity, admin id, ts", "Retention 7y", "—"],
            ["Event — founder_granted_*", "user_id, user_type, founder_number, granted_at, granted_by, reason", "Permanent", "—"],
            ["Event — auto_application_match", "request_id, candidate_id, scores, inclusion/exclusion reason", "Retention 7y", "—"],
            ["Event — assistant_escalation", "conversation transcript + context snapshot", "Retention 7y (escalated); 90d non-escalated", "—"],
        ]},
        "4. Compliance Checklist": {"narrative": "Integrity controls.", "rows": [["Append-only enforced", "No UI or API path can delete audit_logs", "QA", "Pending"], ["Retention purge tested", "Non-audit records purged; audit retained", "QA", "Pending"], ["Backup includes audit", "Daily + cross-region replication", "DevOps", "Verified"]]},
        "5. Assumptions and Constraints": {"narrative": "Retention windows assume current Namibian requirements.", "rows": [["7-year retention", "Compliance with Income Tax Act and Labour Act expectations", "Compliance", "Confirmed"]]},
        "6. Review and Approval": {"narrative": "Attorney sign-off pre-launch.", "rows": [["Attorney", "Sign-off", "External", "Pending"]]},
    },
}
