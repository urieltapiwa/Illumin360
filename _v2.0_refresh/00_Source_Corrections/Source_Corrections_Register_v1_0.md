# Source Document Corrections Register — Illumin360

| Document detail | Value |
|---|---|
| Document title | Illumin360 — Source Corrections Register |
| Document ID | ILLM-V2-000_Source_Corrections_Register |
| Version | 1.0 |
| Date | 14 May 2026 |
| Status | Draft — for editorial sign-off |
| Prepared by | Illumin360 Documentation Workstream |
| Purpose | Records every editorial defect identified in the v3.6 authoritative spec and the v3.6 Disclaimer Master, with the corrected text. This register is the audit trail for the v3.6 → v3.6.1 editorial pass and must be signed off before the v2.0 SDLC refresh propagates corrected source content downstream. |

---

## 1. Defects in `Illumin360_Complete_Technical_Specification_v3.6_Final.md`

### 1.1 Cover-page version inconsistency

The cover page contains three different version markers.

| Location | Current text | Issue | Corrected text |
|---|---|---|---|
| Top header band | `Complete Technical Specification │ CONFIDENTIAL │ v3.3 │ April 2026` | Reads v3.3 | `Complete Technical Specification │ CONFIDENTIAL │ v3.6 │ April 2026` |
| Title block | `COMPLETE TECHNICAL SPECIFICATION  Version 3.2  │  All Modules  │  April 2026` | Reads v3.2 | `COMPLETE TECHNICAL SPECIFICATION  Version 3.6  │  All Modules  │  April 2026` |
| Document detail row "Previous version" | `3.1 — April 2026` | Skips v3.2, v3.3, v3.4, v3.5 | `3.5 — April 2026` |
| Authoritative banner | `Version 3.3 adds full Illumin branding...` | Speaks of v3.3 but doc is v3.6 | `Version 3.6 adds AI-reference anonymisation in client-facing content and the Illumin360 Founder Programme (300 job seekers + 50 employers permanent accounts). All sections from v3.5 remain complete and unchanged.` |

### 1.2 Brand-name typographical errors

The Illumin360 brand is misspelled in five places across the spec.

| Section | Current (incorrect) | Corrected |
|---|---|---|
| 20.9 — D-08 CV Processing Notice (final paragraph) | `Illumingo360 does not share your CV document...` | `Illumin360 does not share your CV document...` |
| 20.11 — D-10 Email Footer (registration line) | `Illumingo360 CC  │  Windhoek, Namibia  │  illumin360.com  │  info@illumin360.com` | `Illumin │ Windhoek, Namibia │ illumin360.com │ info@illumin360.com` |
| 20.12 — D-11 Website General Disclaimer (two occurrences) | `Illumingo360 is not a recruitment agency...` and `Illumingo360 does not independently verify...` and `Illumingo360 CC reserves the right to amend...` | Replace all three with `Illumin360` (and `Illumin` for the CC line if referring to the company rather than the platform) |
| 20.13 — D-12 Data Retention Notice (opening line) | `Illumino360 retains your personal data...` | `Illumin360 retains your personal data...` |

### 1.3 Section 18 — Build Phases stale

Section 18.1 still shows the v3.0 six-phase plan (Phases 1–6, with Phase 6 as "Marketplace expansion"). The authoritative phase plan is in Section 27 (v3.4 onward) which lists eight phases through Phase 8 (RLHF + Marketplace).

| Correction | Action |
|---|---|
| Section 18.1 phase table | Replace with the eight-phase table from Section 27, OR remove Section 18.1 entirely and add a cross-reference: *"See Section 27 for the authoritative phase plan."* |
| Section 18.2 Acceptance Criteria | Verify each criterion still applies under the eight-phase plan; add criteria for AI Assistant, Founder quota enforcement, adaptive weighting sum-to-100, blind-screen photo handling, video transcription weight cap. |

**Recommended action:** delete Section 18.1 and let Section 27 stand as the single source. Section 18.2 acceptance criteria are still useful — expand them rather than remove.

### 1.4 Section 19 — Document Control change log incomplete

Section 19 lists changes through v3.3 only. Three versions are missing from the table.

| Version | Date | Changes (to add) |
|---|---|---|
| 3.4 | April 2026 | Added Section 21 social and community features detail, Section 22 AI Engine Evolution (adaptive weighting, gap analysis, RLHF data collection), Section 23 Video Integration Module (Phase 7), Section 24 Asset Management (employer logos, university logos, blind-screen candidate photos), Section 25 PWA, Section 26 Social Engagement Badges (Verified Student, Compliant Recruiter), Section 27 updated 8-phase plan. |
| 3.5 | April 2026 | Added Section 28 Third-Party AI Services integration specification (Claude Sonnet 4.6 for CV analysis and justification engine; Google Cloud Vision OCR for scanned documents). Added Section 29 Illumin360 AI Platform Assistant — conversational support for all user types using Claude Sonnet 4.6. |
| 3.6 | April 2026 | Added Section 30 Illumin360 Founder Programme (300 permanent job seeker accounts + 50 permanent employer accounts). Added Section 31 Platform Technology Branding Policy — AI/vendor references anonymised across all client-facing content. Updated D-02 wording in Section 31.2 to "proprietary matching system" language. |

### 1.5 Section 19 — authoritative banner stale

The banner immediately after Section 19's change log table still references v3.3.

| Current | Corrected |
|---|---|
| `This document is the single authoritative technical specification for the Illumin360 platform. It is updated whenever a feature is added, modified, or removed. The version number is incremented with every update.` | (unchanged — already version-neutral) |

No action required for this banner; flagged only because adjacent text is being corrected.

### 1.6 Section 13.2 — `student_free` plan_type representation

`Pricing Plans — All Records` lists `student_free` and seven paid plans plus three employer pay-per-use plans. v3.6 introduces a `founder_permanent` plan via Section 30.3.

**Correction:** add row to Section 13.2.

| plan_type | name | base_price | Notes |
|---|---|---|---|
| founder_permanent | Illumin360 Founder — Permanent Subscription | NAD 0.00 | First 300 job seekers — never expires. No invoice generated. Reminder cron skips these. |

### 1.7 Section 17.1 Security — passphrase ambiguity

`Sensitive fields (id_number, student_number, password) encrypted at rest using AES-256` is technically incorrect for passwords. Passwords must be hashed, not encrypted (one-way; bcrypt/argon2). Correction:

> `Sensitive fields (id_number, student_number) encrypted at rest using AES-256. Passwords stored as one-way hashes using argon2id with platform-wide pepper and per-user salt — never encrypted, never reversible.`

### 1.8 Section 28 — Claude model reference verification

Section 28.1 specifies `claude-sonnet-4-6` and stresses pinning to this exact string. This is correct as of the May 2026 issue and requires no change. A reminder note is added in §28.1.5 to validate the model identifier before each release.

---

## 2. Defects in `Illumin360_Disclaimer_Document_v3.6.md`

### 2.1 Cover-page version inconsistency

| Location | Current | Corrected |
|---|---|---|
| Sub-header | `VERSION 1.0 │ APRIL 2026 │ DRAFT FOR ATTORNEY REVIEW` | `VERSION 3.6 │ APRIL 2026 │ DRAFT FOR ATTORNEY REVIEW` |

(Source document filename is v3.6 but the cover sub-header reads v1.0.)

### 2.2 Brand-name typographical errors

Same defects as §1.2 above. Disclaimer master must be corrected to match.

| Section | Current | Corrected |
|---|---|---|
| Section 8 — D-08 CV Processing Notice | `Illumingo360 does not share your CV document...` | `Illumin360 does not share your CV document...` |
| Section 10 — D-10 Email Footer | `Illumingo360 CC` | `Illumin Investments CC` (correct legal entity) |
| Section 11 — D-11 Website Disclaimer (3 occurrences) | `Illumingo360 is not a recruitment agency...`, `Illumingo360 does not independently verify...`, `Illumingo360 CC reserves the right...` | Replace `Illumingo360` with `Illumin360` (platform) or `Illumin` (company) as appropriate |
| Section 12 — D-12 Data Retention | `Illumino360 retains your personal data...` | `Illumin360 retains your personal data...` |

### 2.3 D-02 must be replaced with the v3.6 wording

The disclaimer master's Section 2 contains the pre-v3.6 wording referencing "Illumin360 artificial intelligence matching engine". Section 31.2 of the spec mandates the updated text using "Illumin360 proprietary matching system". The disclaimer master must adopt the §31.2 wording verbatim.

**Action:** replace Disclaimer Document Section 2 D-02 body with the §31.2 v3.6 replacement text.

### 2.4 D-10 — VAT placeholder must be filled

| Current | Corrected |
|---|---|
| `VAT Registration: [VAT NUMBER]` and `[INSERT VAT NUMBER]` | `VAT Registration: 07851437-015` |

This information is available on the spec cover page and should be propagated.

---

## 3. Editorial pass — additional consistency items

| Item | Current | Corrected |
|---|---|---|
| Punctuation consistency | Mixed use of em-dash `—` and pipe `│` as separators | Standardise on em-dash for prose, pipe for header bands only |
| Currency formatting | Mixed `NAD 1,500.00` and `NAD 1500` | Use `NAD 1,500.00` with thousands separator and decimal places throughout |
| "AI" references in client-facing disclaimer text | Disclaimer Document still uses "artificial intelligence matching engine" | Per Section 31, replace with "Illumin360 proprietary matching system" or "Illumin360 matching engine" in client-facing artefacts |
| Smart-quote consistency | Mixed `'` and `'` | Standardise on straight `'` for code blocks, curly `'` for prose |

---

## 4. Sign-off

| Role | Name | Action |
|---|---|---|
| Documentation Lead | TBD | Confirm all defects above are corrected in the v3.6.1 reissue |
| Technical Lead | TBD | Verify §1.6, §1.7 technical corrections |
| Legal — Attorney Reviewer | TBD | Re-approve corrected disclaimer master |
| Project Sponsor | TBD | Authorise the corrected v3.6.1 to supersede v3.6 as source of truth |

Once signed, corrected files are published as:
- `Illumin360_Complete_Technical_Specification_v3.6.1_Final.md` (or `.docx`)
- `Illumin360_Disclaimer_Document_v3.6.1.md` (or `.docx`)

And the v2.0 SDLC refresh proceeds against the corrected source.
