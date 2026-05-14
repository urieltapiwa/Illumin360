# Functional Requirements — v2.0 Refresh Patch

| Document detail | Value |
|---|---|
| Target document | ILLM-02-002_Functional_Requirements (currently v1.0) |
| Patch version | v2.0 |
| Patch date | 14 May 2026 |
| Source authority | Illumin360 Complete Technical Specification v3.6 |
| Patch type | Additive — new FR families and individual requirements |

Numbering convention: existing v1.0 FRs are FR-1.x through FR-N.x (depending on original numbering). This patch introduces new FR families FR-30 through FR-38 to avoid renumbering.

## FR-30 — Founder Programme

| ID | Requirement | Source |
|---|---|---|
| FR-30.1 | The system shall grant Illumin360 Founder status to the first 300 job seeker registrations atomically within the registration transaction. | Spec §30.1 |
| FR-30.2 | The system shall grant Illumin360 Founding Partner status to the first 50 employer registrations atomically within the registration transaction. | Spec §30.2 |
| FR-30.3 | The system shall use a row-level lock (SELECT FOR UPDATE) on the founder count check to prevent two simultaneous registrations both claiming the final slot. | ILLM-03-011 §5 |
| FR-30.4 | Founder records shall include `founder_number` assigned sequentially per user_type. | ILLM-03-011 §4.1 |
| FR-30.5 | A Founder's subscription (job seeker) shall not be subject to expiry or renewal payment. | Spec §30.1 |
| FR-30.6 | A Founder's account shall not be deactivated by the system regardless of activity. Only the Founder themselves may delete the account. | Spec §30.1 |
| FR-30.7 | Once a Founder slot is consumed, it shall not be returned to the pool on account deletion. | ILLM-03-011 §3 |
| FR-30.8 | The system shall expose a public endpoint returning current Founder quota status for display on the homepage. | ILLM-03-011 §10 |
| FR-30.9 | An administrator with capability `founder.override` shall be able to grant Founder status manually with a mandatory written justification (≥30 words). | ILLM-03-011 §6 |
| FR-30.10 | Only a super-administrator shall be able to revoke a Founder badge, and only with documented reason. | ILLM-03-011 §7 |

## FR-31 — Platform Technology Branding Policy

| ID | Requirement | Source |
|---|---|---|
| FR-31.1 | All client-facing artefacts shall use Illumin360 brand terminology in place of "AI", "artificial intelligence", "Claude", "Anthropic", "Google", "machine learning", and "LLM" per ILLM-03-012 §3. | Spec §31.1 |
| FR-31.2 | The Illumin360 Shortlist Report footer shall use the v3.6 D-02 wording referring to "Illumin360 proprietary matching system" (not "artificial intelligence matching engine"). | Spec §31.2 |
| FR-31.3 | The Platform Assistant shall not identify itself as Claude or name any underlying provider when asked. The fallback wording in ILLM-03-013 §7 applies. | ILLM-03-012 §7 |
| FR-31.4 | A post-generation filter shall scan all Platform Assistant responses for prohibited terms and replace or re-prompt before delivery. | ILLM-03-013 §6 |
| FR-31.5 | All 12 disclaimers D-01 through D-12 shall be reviewed for prohibited terminology and updated to v2.0 wording where required. | ILLM-03-012 §6 |

## FR-32 — AI Platform Assistant

| ID | Requirement | Source |
|---|---|---|
| FR-32.1 | The system shall provide five distinct assistant instances: Job Seeker, Employer, Student, Public, Admin. | Spec §29.1 |
| FR-32.2 | Each assistant shall have a separate system prompt, separate data context permissions, and separate audit trail. | ILLM-03-013 §2 |
| FR-32.3 | The Public Assistant shall have no access to any user-specific data. | ILLM-03-013 §3.3 |
| FR-32.4 | The system shall enforce a rate limit of 10 messages per minute per authenticated user and 5 per minute per IP for the public assistant. | ILLM-03-013 §7.5 |
| FR-32.5 | Conversation history shall persist within a browser-tab session only; new sessions start fresh in Phase 6. | Spec §29.3.1 |
| FR-32.6 | The escalate button shall send the full conversation transcript and user context snapshot to info@illumininvestments.com. | Spec §29.3.4 |
| FR-32.7 | Each conversation shall be persisted in `assistant_conversations` with the context snapshot used at session start. | ILLM-03-013 §8.1 |
| FR-32.8 | The platform assistant shall use streaming response (token-by-token) for real-time typing effect. | Spec §29.3.6 |

## FR-33 — Adaptive Weighting

| ID | Requirement | Source |
|---|---|---|
| FR-33.1 | The recruitment request form shall allow employers to specify custom weights for the eight scoring factors. | Spec §22.1 |
| FR-33.2 | Custom weights shall sum to exactly 100. The form shall prevent submission until this condition is met. | Spec §22.1 |
| FR-33.3 | Each custom weight value shall fall within its defined min/max bounds per Section 22.1 of the spec. | Spec §22.1 |
| FR-33.4 | Once a request is submitted, its custom weights shall be immutable (`weights_locked = true`). | ILLM-03-014 §5 |
| FR-33.5 | Every `candidate_matches` row shall record the exact `weights_used` (custom or standard) for that match — immutable audit. | Spec §22.1 |
| FR-33.6 | Shortlist Report Section 5 (Methodology) shall include the weights table and a disclosure statement when custom weights were applied. | ILLM-03-014 §7 |

## FR-34 — Gap Analysis

| ID | Requirement | Source |
|---|---|---|
| FR-34.1 | For candidates scoring between 70% and 85%, the Justification Engine shall produce a gap analysis with two sub-blocks: Areas for consideration and Compensating strengths. | Spec §22.2 |
| FR-34.2 | Gap analysis shall not be produced for candidates ≥85% or <70%. | Spec §22.2 |
| FR-34.3 | Every claim in the gap analysis shall trace to a specific structured field; no hallucinated content. | Spec §22.2 |
| FR-34.4 | Block headings shall be "Areas for consideration" and "Compensating strengths" — no "AI" wording. | ILLM-03-015 §8 |
| FR-34.5 | The anonymous preview screen shall not display gap analysis sub-blocks. | ILLM-03-015 §7 |
| FR-34.6 | Band thresholds (70% lower, 85% upper) shall be admin-configurable. | ILLM-03-015 §9 |

## FR-35 — RLHF Data Collection

| ID | Requirement | Source |
|---|---|---|
| FR-35.1 | An employer shall be prompted via email and dashboard banner to rate match accuracy 14 days after unlocking a report. | Spec §22.3 |
| FR-35.2 | The system shall record feedback in `match_feedback` with `UNIQUE(employer_id, match_id)`. | Spec §22.3 |
| FR-35.3 | Each feedback record shall include `weights_used` and `scoring_model` copied immutably from the match. | Spec §22.3 |
| FR-35.4 | Edits within 30 days of first submission shall update the existing record; later edits are blocked. | ILLM-03-016 §3 |
| FR-35.5 | Phase 6 feedback collection shall not influence scoring or model weights; only data accumulation. | Spec §22.3 |
| FR-35.6 | A model is eligible for Phase 8 refinement only after 500 feedback records have been collected for that scoring model. | Spec §22.3 |
| FR-35.7 | All client-facing labels in the feedback flow shall avoid "AI", "RLHF", or "training" — using "Rate the candidates" or equivalent. | ILLM-03-016 §7 |

## FR-36 — Asset Management

| ID | Requirement | Source |
|---|---|---|
| FR-36.1 | Employers shall be able to upload a company logo (PNG/JPG/SVG, max 5 MB) for use on report covers and the employer profile. | Spec §24.1 |
| FR-36.2 | Job seekers shall be able to upload an optional professional photo (JPG/PNG, max 5 MB). | Spec §24.3 |
| FR-36.3 | Candidate photos shall not be passed to the matching engine and shall not appear in any shortlist or report — anonymous preview or unlocked. | Spec §24.3 |
| FR-36.4 | Candidate photos shall only become visible to an employer who has paid the per-candidate unlock fee. | Spec §24.3 |
| FR-36.5 | Candidate photos shall optionally appear on the public profile card subject to candidate opt-in (default off). | ILLM-03-017 §4.3 |
| FR-36.6 | Institution logos shall be uploadable by admin only and used on co-branded student registration pages and Graduate Spotlight content. | Spec §24.2 |

## FR-37 — Progressive Web App

| ID | Requirement | Source |
|---|---|---|
| FR-37.1 | The platform shall publish a PWA manifest at `/manifest.json` with Illumin theme colour `#1D9E75` and the complete icon set (72–512 px). | Spec §25 |
| FR-37.2 | The platform shall register a service worker for app-shell caching, offline fallback, and update notification. | Spec §25 |
| FR-37.3 | On a new deploy, the user shall see a non-blocking "Reload to update" banner. | ILLM-03-018 §4.2 |
| FR-37.4 | Mutation endpoints (POST/PUT/DELETE) shall never be served from cache; they shall fail gracefully when offline. | ILLM-03-018 §4.1 |
| FR-37.5 | The platform shall expose an "Install Illumin360" button when the browser supports `beforeinstallprompt`. | ILLM-03-018 §5 |
| FR-37.6 | iOS share-to-home-screen shall produce the correct splash, status bar, and title via Apple meta tags. | Spec §25 |

## FR-38 — Social Features and Badges

| ID | Requirement | Source |
|---|---|---|
| FR-38.1 | The platform shall expose a public profile card at `illumin360.com/p/[username]` with HTML, PNG-download, and Open Graph support. | Spec §21 F1 |
| FR-38.2 | A weekly anonymised demand feed shall be displayed on the public homepage with suppression below the minimum-count threshold. | Spec §21 F2 |
| FR-38.3 | The platform shall publish Career Insights articles with SEO metadata and social sharing. | Spec §21 F3 |
| FR-38.4 | The platform shall award and display the badges enumerated in ILLM-03-019. | Spec §21 F4, §26 |
| FR-38.5 | The Graduate Spotlight feature shall publish five featured students per month subject to per-student consent confirmation. | Spec §21 F5 |
| FR-38.6 | Job seekers whose profiles have been unlocked may submit an anonymous rating of the employer 30 days post-unlock; aggregated public reviews shall appear when ≥5 ratings exist. | Spec §21 F6 |
| FR-38.7 | The platform shall provide a referral programme awarding one free month per successful paid referral, with no cap. | Spec §21 F7 |
| FR-38.8 | The platform shall publish an annual Namibia Talent Report email-gated for lead capture. | Spec §21 F8 |
| FR-38.9 | The Verified Student badge shall be awarded automatically by trigger on student verification and removed automatically on rejection/expiry. | Spec §26.1 |
| FR-38.10 | The Compliant Recruiter badge shall be assessed monthly by cron against the §26.2 criteria with admin-only revocation. | Spec §26.2 |

## FR-39 — Video Integration (Phase 7)

| ID | Requirement | Source |
|---|---|---|
| FR-39.1 | Subscribed job seekers and students may upload a video pitch up to 60 seconds, 150 MB, in MP4/MOV/WebM. | Spec §23 |
| FR-39.2 | Uploaded videos shall be virus-scanned, transcribed, and keyword-extracted asynchronously. | Spec §23.1 |
| FR-39.3 | Video transcript keywords shall contribute to scoring at 30% weight relative to CV keywords. | Spec §23.1 |
| FR-39.4 | Videos shall have visibility public or private; private videos appear only after paid candidate unlock. | Spec §23.1 |
| FR-39.5 | Flagged videos shall be removed from public surfaces and enter an admin moderation queue. | ILLM-03-021 §9 |

## Change log

| Version | Date | Changes |
|---|---|---|
| 1.0 | (existing) | Initial issue |
| 2.0 | 14 May 2026 | Added FR-30 through FR-39 covering Founder Programme, branding policy, AI Assistant, adaptive weighting, gap analysis, RLHF, asset management, PWA, social features and badges, video integration. |
