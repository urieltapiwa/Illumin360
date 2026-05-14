# Skill-Gap and Growth Narrative — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 Gap Analysis — Detailed Design |
| Document ID | ILLM-03-015_Gap_Analysis_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft |
| Source authority | Section 22.2, Illumin360 Complete Technical Specification v3.6 |
| Phase | Phase 6 |
| Owner | Platform Engineering — Matching Engine + Report Generation |

## 1. Purpose

The Illumin360 Justification Engine adds a Candidate Gap Analysis for shortlisted candidates whose total match score falls in the 70–85% band. The analysis identifies what the candidate is missing against the requirements and what compensating strengths offset those gaps. Candidates above 85% receive a standard strong-match justification; candidates below 70% are not shortlisted and no gap analysis is generated.

The intent is to give the employer a clearer picture of "almost matches" — candidates who would be strong with development — and to support fair, transparent reasoning for inclusion in the shortlist.

## 2. Band logic

| Match score | Output |
|---|---|
| ≥ 85% | Standard strong-match justification — no gap analysis block |
| 70–85% (inclusive lower, exclusive upper, so 70.00–84.99) | Standard justification **plus** gap analysis sub-blocks |
| < 70% | Not shortlisted — no gap analysis generated |

Band thresholds are stored in platform settings and adjustable by admin. Default values per Section 22.2 of the spec.

## 3. Gap and compensation categories

| Category | Example output |
|---|---|
| Missing qualification | "Candidate holds a Diploma (NQF 6) rather than the required Degree (NQF 7). This is a one-level qualification gap against the stated minimum." |
| Missing skill | "Candidate profile does not list SAP Financial Accounting, identified as a key requirement. No SAP exposure found in CV text." |
| Experience shortfall | "Candidate has 3 years of relevant experience against the minimum of 5 years — a gap of approximately 2 years." |
| Missing certification | "Candidate does not hold the required OHS certification. No equivalent found in profile." |
| Missing language | "Candidate does not list Afrikaans against the request's language requirement." |
| Compensating strength — high skills | "Experience shortfall is partially offset by strong skills alignment (94%) and all five required technical tools explicitly listed." |
| Compensating strength — strong experience | "Qualification gap is partially offset by 8 years of directly relevant industry experience, exceeding the minimum by 3 years." |
| Compensating strength — certifications | "Missing degree is partially offset by holding both required professional certifications." |
| Compensating strength — recency | "Recent CV (uploaded within 30 days) and an active subscription status indicate a candidate currently engaged with the job market." |

The list above is non-exhaustive. Each gap and compensation entry is structured (category code + free-text human description) so downstream analytics and the report can group them.

## 4. Data model

### 4.1 Additions to `candidate_matches`

| Column | Type | Constraints | Description |
|---|---|---|---|
| gap_analysis | JSONB | NULL allowed | Populated only when match score is in the 70–85% band. NULL otherwise. Structure: `{gaps: [{category, description, severity}], compensations: [{category, description}], generated_at}`. |
| gap_analysis_displayed | BOOLEAN | DEFAULT false | True after the report containing this match was generated and includes the gap analysis. Used for QA and analytics. |

Severity is one of `minor`, `moderate`, `significant` — for sorting in the report. The classification is rule-based: missing skill = moderate; missing required certification = significant; one-level qualification gap = moderate; two-level qualification gap = significant; experience shortfall ≤ 1 year = minor, 2–3 years = moderate, > 3 years = significant.

## 5. Generation algorithm

Gap analysis runs immediately after scoring for any candidate in the 70–85% band. The algorithm is deterministic for the structured comparison and uses the matching engine output for the natural-language description.

```
if 0.70 <= match_score < 0.85:
    gaps = []
    compensations = []

    # Structured comparison — deterministic
    if candidate.qualification_nqf < request.min_nqf:
        gaps.append({
            category: "qualification",
            description: format_qualification_gap(...),
            severity: classify_qualification_severity(...)
        })

    if candidate.skills_match_pct < 0.80:
        missing = request.required_skills - candidate.skills
        for skill in missing:
            gaps.append({...})

    if candidate.experience_years < request.min_experience:
        gaps.append({...})

    # ... (other categories)

    # Compensations — derived from per-factor scores
    if "qualification" in [g.category for g in gaps]:
        if candidate.experience_score > 0.85:
            compensations.append({...})
        if candidate.certifications_score > 0.85:
            compensations.append({...})

    # Natural-language polish via justification engine (per Section 28.1)
    final = polish_with_justification_engine(gaps, compensations, candidate, request)

    candidate_match.gap_analysis = final
```

The natural-language polish step ensures the descriptions read fluently for the report. Every claim in the polished output must still trace to a specific structured field — no hallucinated gaps. The justification engine is instructed explicitly to not invent gaps that are not present in the structured input.

## 6. Report integration

Section 3 of the shortlist report (Top Shortlist Candidates — full detail cards) renders the gap analysis as two clearly labelled sub-blocks for candidates in the 70–85% band:

> **Areas for consideration**
> - Candidate holds a Diploma (NQF 6) rather than the required Degree (NQF 7) — a one-level qualification gap.
> - Candidate's profile does not list SAP Financial Accounting; no SAP exposure found in CV text.
>
> **Compensating strengths**
> - 8 years of directly relevant industry experience, exceeding the minimum by 3 years.
> - Strong skills alignment (94%) with all five required technical tools explicitly listed.

Strong-match candidates (≥ 85%) and weak candidates (< 70%, not shortlisted) do not show these sub-blocks. The report's introduction does not need to flag whether a candidate fell in the gap-analysis band — the presence or absence of the sub-blocks self-discloses.

## 7. UI integration — preview screen

The anonymous preview screen (before payment) shows match scores and Candidate A/B/C/D labels. The gap analysis sub-blocks are **not** shown in the anonymous preview — they reveal too much about the candidate. Sub-blocks become visible only after the report is unlocked.

## 8. Branding compliance

Per Section 31 branding policy, the report block heading is **"Areas for consideration"**, not "AI-identified gaps", "AI gap analysis", or "machine-suggested gaps". The block label is intentionally human-readable and neutral.

The natural-language descriptions avoid first-person AI framing ("the AI thinks...") and adopt a recruiter-style voice ("Candidate holds...", "Experience shortfall...").

## 9. Admin controls

| Setting | Default | Description |
|---|---|---|
| `gap_analysis.lower_band` | 0.70 | Minimum match score for gap analysis generation |
| `gap_analysis.upper_band` | 0.85 | Maximum match score (exclusive) — above this is strong match |
| `gap_analysis.max_gaps_displayed` | 4 | Maximum gap entries shown in report — top severities |
| `gap_analysis.max_compensations_displayed` | 3 | Maximum compensation entries shown in report |

Settings are admin-only, editable from the platform configuration page, audited on change.

## 10. Acceptance criteria

1. Candidate with match score 85.00% does not have gap analysis generated.
2. Candidate with match score 84.99% has gap analysis generated.
3. Candidate with match score 70.00% has gap analysis generated.
4. Candidate with match score 69.99% has no gap analysis and is not shortlisted.
5. Every claim in the rendered output traces to a structured field (e.g., qualification gap claim corresponds to candidate.qualification_nqf < request.min_nqf).
6. Report Section 3 renders "Areas for consideration" and "Compensating strengths" sub-blocks for 70–85% band candidates only.
7. Anonymous preview screen does not show gap analysis text.
8. Block labels match §6 wording — never use "AI" in the headings.

## 11. Cross-references

| Document | Section |
|---|---|
| Spec v3.6 | Section 22.2 (canonical), Section 8.3 (justification engine), Section 10 (report) |
| AI Services Design (ILLM-03-008 v2.0) | Justification engine integration |
| Adaptive Weighting Design (ILLM-03-014) | Companion Phase 6 feature |
| Database Design (ILLM-03-004 v2.0) | candidate_matches.gap_analysis column |
| Branding Policy (ILLM-03-012) | Heading and language compliance |

## 12. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. |
