# Adaptive Employer-Driven Weighting — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 Adaptive Weighting — Detailed Design |
| Document ID | ILLM-03-014_Adaptive_Weighting_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft |
| Source authority | Section 22.1, Illumin360 Complete Technical Specification v3.6 |
| Phase | Phase 6 |
| Owner | Platform Engineering — Matching Engine |

## 1. Purpose

Employers may boost or suppress specific scoring weights when creating a recruitment request to reflect role priorities (for example, a senior role weighting experience higher; a graduate role weighting qualification higher). This document specifies the data model, validation rules, scoring impact, audit trail, and report disclosure for adaptive weighting.

Adaptive weighting is **opt-in**. A `NULL` value in `custom_weights` means standard platform weights apply — fully backward compatible.

## 2. Weight bounds

Custom weights must sum to exactly 100% and each factor must fall within its allowed band.

| Factor | Default | Min allowed | Max allowed |
|---|---|---|---|
| Qualification match | 20% | 5% | 50% |
| Skills alignment | 20% | 5% | 50% |
| Experience relevance | 15% | 5% | 40% |
| Location fit | 15% | 5% | 35% |
| Availability status | 10% | 0% | 25% |
| Language fit | 8% | 0% | 25% |
| Certifications | 7% | 0% | 30% |
| CV recency | 5% | 0% | 15% |

The bounds prevent extreme single-factor dominance that would undermine the legal defensibility of the shortlist as a multi-factor assessment.

## 3. Data model

### 3.1 Additions to `recruitment_requests`

| Column | Type | Constraints | Description |
|---|---|---|---|
| custom_weights | JSONB | NULL allowed | `{qualification:25, skills:20, experience:20, location:10, availability:8, language:5, certifications:7, cv_recency:5}` — keys match factor names. NULL = standard. |
| weights_locked | BOOLEAN | DEFAULT false | Set true on request submission. Cannot be changed after lock. |

### 3.2 Additions to `candidate_matches`

| Column | Type | Constraints | Description |
|---|---|---|---|
| weights_used | JSONB | NOT NULL | Immutable record of the exact weights used to generate this match. Stored verbatim for audit and report methodology. |

Existing scoring fields (per_factor scores, total_score) remain unchanged.

## 4. Validation rules

Validation runs server-side on `POST /requests` and `PUT /requests/:id` (while draft).

| Rule | Behaviour on violation |
|---|---|
| Sum of all values = 100 (integer) | 400 Bad Request — message: "Weights must sum to exactly 100. Current total: {sum}." |
| Each factor value within bounds (§2) | 400 — "{factor} must be between {min} and {max}." |
| All eight factors present | 400 — "Missing factor: {factor}." Sets default for unknown keys is rejected to prevent silent assumptions. |
| Integer values only | 400 — "Weights must be integers." Decimal weights would invite rounding issues. |
| Request status not already submitted | 400 — "Request weights are locked after submission." |

Validation runs before any persistence. Front-end live-validates the same rules and prevents submission until valid.

## 5. Lock behaviour

When a recruitment request transitions from `draft` to `submitted`, `weights_locked` is set to true atomically with the status change. Any subsequent attempt to modify `custom_weights` returns 400. This ensures the matching that runs against the request is reproducible from the locked weights.

If an employer needs different weights for a similar role, they create a new request rather than editing the locked one.

## 6. Scoring engine integration

Scoring code reads weights as follows:

```
weights = request.custom_weights if request.custom_weights is not None else STANDARD_WEIGHTS
score = sum(per_factor[factor] * weights[factor] / 100 for factor in FACTORS)
```

`STANDARD_WEIGHTS` is the Section 8.2 default schedule. The student scoring model (different default weights for student requests) is a separate weight schedule and remains untouched by adaptive weighting in Phase 6. Custom weighting against student requests is permitted; the same bounds apply.

For every candidate match generated, `weights_used` is populated with the exact weights — even if standard. This decouples the audit trail from the request record: if defaults later change, the match record continues to show the weights actually applied at the time.

## 7. Report disclosure

Section 5 of the shortlist report (Methodology Disclosure) shows the actual weights used. The presence of custom weights triggers an explicit statement in the methodology section:

> *"The employer specified custom weighting for this request. The weights applied are shown above. The match scores and rankings reflect these custom weights, not the platform's default weighting schedule."*

When standard weights apply, the methodology section reads:

> *"This request used the platform's standard weighting schedule. The weights applied are shown above."*

In both cases, the per-factor weight table appears in the methodology section so the report is self-describing and legally defensible without external reference.

## 8. UI implementation

### 8.1 Where the control appears

Adaptive weighting is exposed in the recruitment request form at Step 3 (Filters and Weighting). It is a collapsible section labelled *"Customise scoring weights (optional)"*. Collapsed by default — standard weights show as the active configuration.

### 8.2 Control design

| Element | Behaviour |
|---|---|
| Eight sliders | One per factor, with min/max bounds enforced by the slider track. Numeric input next to each slider. |
| Live sum display | "Total: 100" — green when valid, red when not 100. Real-time. |
| Auto-balance helper | Optional "Auto-balance" button that proportionally adjusts other factors when one is moved beyond its share — bound to a single factor's manual change. Off by default. |
| Reset button | Returns all sliders to platform defaults. |
| Lock notice | Persistent notice: "Weights cannot be changed after request submission." |

### 8.3 Visual indication of customisation

After a custom-weighted request is submitted, the request card on the employer dashboard shows a small "Custom weights" tag next to the title.

## 9. API surface

| Method | Endpoint | Description |
|---|---|---|
| POST | /employers/me/requests | Body includes optional `custom_weights` object. Validation per §4. |
| PUT | /employers/me/requests/:id | Allowed only while status = draft. After submission, custom_weights cannot change. |
| GET | /employers/me/requests/:id | Response includes `custom_weights` and `weights_locked`. |
| GET | /employers/me/requests/:id/matches | Each match includes `weights_used`. |
| GET | /employers/me/requests/:id/report | Report Section 5 includes the weights table and disclosure statement (§7). |

## 10. Migration and backward compatibility

Existing requests created before this feature have `custom_weights = NULL`. They continue to score against standard weights with no change. The migration adds:

```
ALTER TABLE recruitment_requests
  ADD COLUMN custom_weights JSONB NULL,
  ADD COLUMN weights_locked BOOLEAN NOT NULL DEFAULT false;

ALTER TABLE candidate_matches
  ADD COLUMN weights_used JSONB NULL;
```

Then a backfill populates `weights_used` for existing candidate_matches with the standard weights schedule that applied at the time of their generation (captured from a single static record). After backfill, `weights_used` is set NOT NULL via a follow-on migration.

## 11. Acceptance criteria

1. An employer can create a request with custom weights summing to exactly 100, each within bounds.
2. Server rejects requests with sums other than 100 or with any factor out of bounds.
3. After submission, custom_weights cannot be modified — 400 returned on attempts.
4. Match generation uses the request's custom_weights when present, otherwise standard weights.
5. Each candidate_matches row stores weights_used reflecting the exact weights applied.
6. Report Section 5 includes the weights table and the §7 disclosure statement.
7. Existing requests (NULL custom_weights) continue to match correctly with standard weights.
8. The UI prevents submission while the sum ≠ 100 or any factor is out of bounds.

## 12. Test cases (high-level)

- Boundary: weights at exact min and max — accepted.
- Boundary: weights at min−1 or max+1 — rejected.
- Sum = 99: rejected.
- Sum = 100, but one factor missing: rejected.
- Sum = 100 with decimals: rejected.
- Submit then attempt PUT custom_weights: rejected.
- Compare scores: same candidate against same request with two different custom_weights produces predictable score differences.
- Existing request (NULL): unchanged behaviour.

## 13. Cross-references

| Document | Section |
|---|---|
| Spec v3.6 | Section 22.1 (canonical), Section 8 (matching engine), Section 10 (report) |
| Database Design (ILLM-03-004 v2.0) | Schema changes from §3 |
| API Design (ILLM-03-005 v2.0) | Endpoint changes from §9 |
| Gap Analysis Design (ILLM-03-015) | Companion Phase 6 feature |
| Report Generation Module | Section 5 disclosure block |

## 14. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. |
