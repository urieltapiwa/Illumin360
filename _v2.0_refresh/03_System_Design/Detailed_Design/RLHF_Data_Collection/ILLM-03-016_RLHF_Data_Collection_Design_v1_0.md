# RLHF Data Collection — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 RLHF Data Collection — Detailed Design |
| Document ID | ILLM-03-016_RLHF_Data_Collection_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft |
| Source authority | Section 22.3, Illumin360 Complete Technical Specification v3.6 |
| Phase | Phase 6 (collection infrastructure) → Phase 8 (model refinement) |
| Owner | Platform Engineering — Matching Engine |

## 1. Purpose

This document specifies the data collection infrastructure for employer feedback on match accuracy. Phase 6 builds collection only — no model adjustments occur in Phase 6. Once at least 500 feedback records per scoring model are accumulated, Phase 8 uses the dataset for model refinement.

Branding policy applies: the user-facing label is **"Rate the candidates"** or **"How accurate was this shortlist?"** — never "RLHF", "machine learning feedback", or "AI training data" in client-facing text.

## 2. Collection trigger

Employers are prompted to rate match accuracy **14 days after** unlocking a report. The 14-day delay allows the employer to interview, advance, or reject candidates before rating accuracy — feedback before interviews is uninformed.

Three collection surfaces:

| Surface | Channel |
|---|---|
| Email link | Day-14 email to employer with a tokenised deep link to a rating page |
| Dashboard prompt | Subtle banner on the employer dashboard showing "Rate this shortlist" for reports unlocked > 14 days ago, dismissable |
| Manual | Available indefinitely via the report detail page once the 14-day window opens |

Feedback can be submitted once per `(employer, match)` pair — `UNIQUE(employer_id, match_id)` enforces. Updates within 30 days of first submission are permitted; the latest entry replaces.

## 3. Data model

### 3.1 New table — `match_feedback`

| Column | Type | Constraints | Description |
|---|---|---|---|
| id | UUID PK | | |
| request_id | UUID FK | NOT NULL | Recruitment request |
| match_id | UUID FK | NOT NULL | Specific candidate match |
| employer_id | UUID FK | NOT NULL | Employer submitting feedback |
| accuracy_rating | INTEGER | NOT NULL, 1–5 | Overall match accuracy |
| justification_rating | INTEGER | NOT NULL, 1–5 | Quality of the candidate analysis paragraph |
| employer_notes | TEXT | NULL allowed | Optional free-text explanation |
| scoring_model | VARCHAR | NOT NULL | `standard`, `student`, `graduate_programme` — copied from the request at submission |
| weights_used | JSONB | NOT NULL | Copied from the corresponding candidate_matches.weights_used at submission — immutable record of weights at time of match |
| industry | VARCHAR | NULL | Copied from request — for analytics grouping |
| role_category | VARCHAR | NULL | Copied from request — for analytics grouping |
| feedback_source | ENUM | NOT NULL | `email_link`, `dashboard_prompt`, `manual` |
| created_at | TIMESTAMPTZ | NOT NULL DEFAULT now() | |
| updated_at | TIMESTAMPTZ | NULL | Set on edit within 30 days |

Constraints: `UNIQUE(employer_id, match_id)`.

Indexes: `(scoring_model, created_at)` for analytics; `(industry, role_category)` for segmented analysis; `(request_id)` for retrieving full request feedback.

Retention: 7 years per Section 15.3 (audit/compliance retention).

## 4. API endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | /employers/me/requests/:id/feedback | Bearer (employer) | Body: array of `{match_id, accuracy_rating, justification_rating, notes}`. Validates ownership of the request. Idempotent per (employer, match). |
| GET | /employers/me/requests/:id/feedback | Bearer (employer) | Returns the employer's own feedback for this request. |
| GET | /admin/feedback | Admin | All feedback, filterable by industry, scoring_model, date range, rating thresholds. Pagination. |
| GET | /admin/feedback/summary | Admin | Aggregated analytics: average ratings per factor, per scoring model, per industry. Used as the input for Phase 8 planning. |
| GET | /admin/feedback/coverage | Admin | Counts toward the 500-record threshold per scoring model. Displays whether Phase 8 can proceed. |

## 5. Email collection flow

The day-14 prompt email is sent by the daily cron at 06:00 WAT (existing cadence).

| Detail | Value |
|---|---|
| Email template ID | `feedback_request_employer` |
| Subject | "How accurate was your recent shortlist?" |
| Send window | 14 days after `reports.unlocked_at` |
| Tokenised link | One-time deep link expiring after 30 days |
| Branding | Per Section 31 — no AI references in the body |

The token is bound to the employer and the request; clicking it logs the employer in and surfaces the feedback form pre-filled for the unlocked candidates from that report. Submission posts to `/employers/me/requests/:id/feedback`.

## 6. UI design

### 6.1 Feedback form

Per candidate from the unlocked report:
- Candidate name (read-only, from the unlocked report)
- "How accurate was this match?" — five-star control (1 lowest, 5 highest)
- "How useful was the candidate analysis?" — five-star control
- "Notes (optional)" — single-line text input, 240 char limit

Bulk submit at the bottom: "Submit all ratings". A "Skip this candidate" link permits selective rating.

### 6.2 Dashboard prompt

Banner shown above the dashboard's recent reports section when the user has unrated reports older than 14 days:

> *"You unlocked **Senior Accountant — Windhoek** on 14 April. How accurate was the shortlist? **Rate now →**"*

Dismissable per report — dismissal logged. The prompt does not re-appear for that report once dismissed.

## 7. Branding compliance

Per Section 31:
- Field labels: "How accurate was this match?", "Rate the candidate analysis", "Notes"
- Never: "AI accuracy", "AI feedback", "How well did the AI rank...", "Help us train..."
- The feedback request email subject line uses plain-English framing: "How accurate was your recent shortlist?"

The dashboard banner and email body explain the purpose as platform-improvement, not AI-training: "Your feedback helps us improve future shortlist quality."

## 8. Analytics — Phase 8 readiness gate

A separate `feedback_coverage` view aggregates record counts per scoring model:

```
SELECT scoring_model, COUNT(*) AS records
  FROM match_feedback
 GROUP BY scoring_model;
```

The admin dashboard widget displays:

> Standard model: 312 of 500 records collected (62%)
> Student model: 45 of 500 records collected (9%)
> Graduate programme model: 12 of 500 records collected (2%)

Once a model reaches 500 records, an alert fires to the platform owner: "Phase 8 model refinement is now possible for the {model} scoring model." Refinement itself is out of scope for this design — Phase 8 has its own design document.

## 9. Anti-abuse

| Risk | Mitigation |
|---|---|
| Employer rates all matches as 1-star to lower a competitor's rank (irrelevant — feedback is not exposed to other employers, but could influence model refinement) | Phase 8 refinement uses outlier filtering and per-employer normalisation before any aggregation |
| Same employer rates the same match twice with different values | UNIQUE(employer, match) enforces; updates within 30 days allowed and visible in audit log |
| Employer rates without having interviewed candidates | Acceptable for Phase 6 collection. Phase 8 refinement can weight feedback by employer activity signals. |
| Token reuse / hijack | Tokens are single-use, expire after 30 days, bound to a specific request_id and employer_id, signed with HMAC |

## 10. Audit trail

Every feedback submission is logged in `audit_logs`:

| Event | Logged fields |
|---|---|
| feedback_submitted | employer_id, request_id, match_id, accuracy_rating, justification_rating, feedback_source, IP, user_agent |
| feedback_updated | as above plus previous values |
| feedback_email_sent | employer_id, request_id, email_template_id, sent_at |
| feedback_email_clicked | employer_id, request_id, clicked_at |
| feedback_dashboard_dismissed | employer_id, request_id, dismissed_at |

## 11. Acceptance criteria

1. The day-14 cron sends `feedback_request_employer` exactly once per unlocked report, only after the 14-day window has elapsed.
2. Tokenised email links resolve to the feedback form with the correct request and candidates pre-loaded.
3. Submitting feedback inserts one row per candidate into `match_feedback`. UNIQUE(employer, match) prevents duplicates.
4. Edits within 30 days of first submission update the existing row, not insert a new one.
5. Admin coverage widget accurately reflects per-model counts and triggers the Phase 8-ready alert at exactly 500.
6. No client-facing text references "AI", "RLHF", "model training", or any third-party provider.
7. Feedback is retained for 7 years and cannot be deleted by employer action.

## 12. Cross-references

| Document | Section |
|---|---|
| Spec v3.6 | Section 22.3 (canonical) |
| Adaptive Weighting Design (ILLM-03-014) | weights_used is sourced from candidate_matches |
| Database Design (ILLM-03-004 v2.0) | match_feedback table |
| API Design (ILLM-03-005 v2.0) | Feedback endpoints |
| Branding Policy (ILLM-03-012) | Language compliance |
| Phase 8 RLHF Marketplace doc (to be created) | Model refinement workflow |

## 13. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. |
