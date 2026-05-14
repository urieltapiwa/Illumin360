# Platform Technology Branding Policy

| Document detail | Value |
|---|---|
| Document title | Illumin360 — Platform Technology Branding Policy |
| Document ID | ILLM-03-012_Platform_Branding_Policy |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft — for executive sign-off |
| Source authority | Section 31, Illumin360 Complete Technical Specification v3.6 |
| Applies to | All client-facing artefacts produced by or on behalf of Illumin Investments CC for the Illumin360 platform |
| Owner | Marketing & Communications, jointly with Platform Engineering |

## 1. Purpose

This policy establishes mandatory branding standards for all Illumin360 client-facing content. The underlying technology stack — including third-party AI services — is proprietary, confidential, and not referenced in any client-facing artefact. Clients buy the Illumin360 platform; they do not need to know, and are not told, what powers it.

## 2. Why this policy exists

| Driver | Rationale |
|---|---|
| Competitive protection | A competitor who can read on Illumin360's marketing pages exactly which AI model and vendors are used can replicate the technology stack and undercut. |
| Brand consistency | Every client touchpoint reinforces the Illumin360 brand. Mentions of "Claude" or "AI" dilute it. |
| Legal and reputational | Decoupling from named vendors insulates Illumin360 from changes, outages, or controversies affecting any single provider. |
| Future flexibility | The stack may change. Marketing language should be model-agnostic so a change of underlying provider is invisible to customers. |

## 3. Language standards — prohibited vs correct

The following terms must not appear in any client-facing artefact. Each row gives the prohibited term and the correct Illumin360 substitution.

| Prohibited in client-facing content | Correct Illumin360 language |
|---|---|
| AI Shortlist Report | Illumin360 Shortlist Report |
| AI-powered matching | Illumin360 matching |
| AI matching engine | Illumin360 matching engine |
| AI-generated report | Illumin360-generated report |
| AI justification | Candidate analysis |
| Automated AI processing | Illumin360 automated processing |
| Powered by Claude | (do not use any "Powered by" attribution) |
| Powered by AI | Illumin360 |
| Claude / Claude Sonnet / Claude Sonnet 4.6 / any model name | Never mentioned to clients |
| Anthropic | Never mentioned to clients |
| OpenAI / GPT / any model name | Never mentioned to clients |
| Google Cloud Vision / Google Cloud | Never mentioned to clients |
| Machine learning / ML | Illumin360 intelligent matching |
| Natural language processing / NLP | Illumin360 language understanding (avoid technical jargon — prefer "reads CVs the way a recruiter would") |
| Large language model / LLM | Never mentioned to clients |
| Artificial intelligence | Illumin360 |
| Algorithm / algorithmic | Illumin360 matching system (avoid "algorithm" in client-facing copy except where legally required for transparency disclosures) |

### 3.1 Permitted technical language

Some technical language is required for legal defensibility, particularly in disclaimers. The following terms are permitted in client-facing artefacts where legally necessary:

| Term | Where permitted | Rationale |
|---|---|---|
| Automated data processing | Disclaimers D-02, D-05, D-07 | Required for transparency under data protection law |
| Automated decision-making | D-05 candidate consent | Required for transparency under data protection law |
| Proprietary matching system | D-02, marketing | Conveys the system is technical and automated without naming any provider |
| Illumin360 matching engine | All artefacts | Brand-internalised technical reference |
| Illumin360 proprietary technology | All artefacts | Generic permitted reference |
| Smart / intelligent | All artefacts | Plain-English description |

## 4. Scope of application

The policy applies in full to every artefact in the left column. The right column lists internal-only artefacts that are exempt because they are not client-facing.

| Policy applies (client-facing) | Exempt (internal use only) |
|---|---|
| All client emails and notifications | Technical specification document |
| All shortlist reports — PDF and Word | Developer API integration documentation |
| All invoices and receipts | Internal cost-tracking and analytics dashboards |
| All platform UI — dashboards, forms, modals, error states | Vendor contracts and service agreements |
| All public website content | Attorney privileged communications |
| All social media and marketing materials | Admin dashboard cost monitoring (internal admin only) |
| All disclaimers D-01 through D-12 | Architecture decision records (ADRs) |
| The Illumin360 Platform Assistant — system prompts must instruct the assistant to never identify itself as Claude, never reference Anthropic, and never explain its underlying model | Penetration testing reports |
| Sales talking points | Operations runbooks |
| Training materials (when distributed to clients) | Internal training materials (engineering, ops) |
| FAQs / Knowledge base | — |
| Support email signatures | — |
| Push notifications | — |
| SMS templates | — |
| Job postings and recruitment ads for Illumin Investments CC roles | (engineering job posts may name the stack to attract qualified engineers — confirm with marketing) |

## 5. Updated D-02 disclaimer text — for implementation

The v3.6 spec mandates a replacement of the report footer disclaimer (D-02) to remove all "artificial intelligence" wording while remaining legally sound. This is the canonical text to be used:

> **Illumin360 Shortlist Report Disclaimer**
>
> This report has been generated by the Illumin360 proprietary matching system using automated data processing. The rankings, match scores, and candidate analyses contained in this report are produced by an automated system and do not constitute professional recruitment advice, a recommendation to employ any specific individual, or an assessment of any candidate's character, reliability, or suitability beyond the documented criteria specified in the recruitment request.
>
> The match scores and rankings are based solely on the structured profile data and CV content provided by candidates on the Illumin360 platform and are subject to the accuracy and completeness of that self-declared information. Illumin360 does not independently verify candidate qualifications, employment history, or any other information provided by candidates.
>
> This report is provided as a decision-support tool only. The employer is solely responsible for conducting appropriate due diligence, including verification of qualifications and references, and for all hiring decisions made. Illumin accepts no liability for any loss, damage, or adverse outcome arising from reliance on this report.
>
> This report is confidential and intended solely for the authorised recipient.
>
> Prepared by: Illumin360 │ Illumin Investments CC │ Windhoek, Namibia │ www.illumininvestments.com

This text supersedes the earlier wording in D-02. All previously-generated reports retain their original footer text — re-generation is not retroactive.

## 6. Implementation checklist

| Area | Action | Owner | Status |
|---|---|---|---|
| Report templates (PDF + Word) | Replace D-02 footer with §5 text | Engineering | Pending |
| Public website copy | Audit all pages — replace prohibited terms per §3 | Marketing | Pending |
| Email templates (33 templates per Section 12) | Audit all subjects, bodies, footers — replace prohibited terms | Marketing + Engineering | Pending |
| Platform UI strings | i18n strings file audit — replace all `ai_*` keys and English values | Engineering | Pending |
| All 12 disclaimers | Audit D-01 through D-12 — confirm no prohibited terms remain after D-02 update | Legal + Marketing | Pending |
| Sales talking points | Full rewrite per §3 | Sales / Marketing | Pending |
| Training materials | Audit and replace per §3 | Training | Pending |
| FAQ / Knowledge base | Audit and replace per §3 | Support | Pending |
| Platform Assistant system prompts | Add prohibition to system prompt of every assistant instance (job seeker, employer, student, public, admin) — assistant must refuse to discuss its underlying model | Engineering | Pending |
| Public assistant — public homepage chat widget | Specific care: visitors may explicitly ask "what AI are you?" — assistant must respond per §7 below | Engineering | Pending |
| Marketing collateral (PDFs, decks, brochures) | Audit and replace | Marketing | Pending |
| Social media bios and pinned posts | Audit and replace | Marketing | Pending |
| Press releases (past and future) | Audit and replace where possible; future releases must follow policy | Marketing | Pending |

## 7. Assistant identity policy

The Illumin360 Platform Assistant (Section 29 of the spec, ILLM-03-013 detailed design) is the most sensitive enforcement point because users will sometimes explicitly probe what powers it. The assistant's system prompt must include instructions equivalent to the following:

> You are the Illumin360 Platform Assistant. You are part of the Illumin360 platform built by Illumin Investments CC.
>
> You must not identify yourself as Claude, or any other named AI model. You must not name Anthropic, OpenAI, Google, or any third-party technology provider. If asked "what AI are you?" or "are you Claude?" or "what model are you?", respond with:
>
> *"I'm the Illumin360 Platform Assistant. I'm built to help you with the Illumin360 platform specifically — your profile, requests, billing, and questions about how the platform works. I'm not able to discuss the technology that powers me."*
>
> Do not deny that you are an automated system — automated decision-making transparency is a legal requirement. Confirm you are automated; do not name what underlies you.

This wording reconciles the branding policy with the legal transparency requirement: users are correctly informed that they are speaking to an automated system, but the underlying provider is not disclosed.

## 8. Enforcement and exceptions

### 8.1 Pre-publication review

All client-facing artefacts produced after this policy's effective date pass through a branding policy check before publication. The check is a single page that runs through the prohibited-terms list in §3 and verifies none appear.

### 8.2 Exception process

Exceptions may be granted only by the platform owner in writing and only for specific narrowly-scoped artefacts. Documented exceptions are recorded in an exception register held by Marketing.

Likely legitimate exceptions:
- Engineering recruitment job posts naming the stack to attract candidates
- Investor materials (under NDA)
- Vendor contract negotiations (not client-facing)
- Legal discovery and litigation contexts

### 8.3 Audit cadence

The full client-facing surface is audited quarterly against this policy. The audit covers website pages, email templates, in-product UI strings, disclaimers, knowledge base, sales materials, and the assistant's response sample. Findings are recorded in a remediation log and assigned to owners with a 30-day correction window.

## 9. Acceptance criteria

1. No client-facing artefact contains any term in the §3 prohibited list.
2. D-02 disclaimer reads exactly the §5 text in all generated reports.
3. The Platform Assistant, when explicitly asked what AI model powers it, responds with the §7 wording or close paraphrase that names neither Claude nor Anthropic.
4. Engineering recruitment materials may name the technical stack only with documented Marketing approval recorded in the exception register.
5. A quarterly audit log exists with findings and remediation tracking.

## 10. Cross-references

| Document | Section / Location |
|---|---|
| Illumin360 Complete Technical Specification v3.6 | Section 31 (canonical), Section 28 (third-party AI internal-only), Section 29 (Platform Assistant) |
| Disclaimer Master v3.6.1 | All 12 disclaimers — D-02 in particular is replaced by §5 of this policy |
| UI/UX Design (ILLM-03-006 v2.0) | i18n string compliance |
| Report Generation Module (per Section 10 of spec) | Template footer compliance |
| Sales Talking Points (ILLM-12-007 v2.0) | Sales language compliance |
| AI Platform Assistant Design (ILLM-03-013) | System prompt §7 compliance |
| Branding Component Library (ILLM-12-008 v2.0) | Copy patterns for compliant client-facing language |

## 11. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. Created to address coverage gap — the Section 31 branding policy had no owning policy document. |
