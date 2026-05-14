# Social Engagement Badges — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 Social Engagement Badges — Detailed Design |
| Document ID | ILLM-03-019_Social_Engagement_Badges_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft |
| Source authority | Section 21 (F4 badges), Section 26 (v3.4 new badges), Section 30 (Founder badges) |
| Phase | Phase 5 (core badges) + Phase 6 (v3.4 additions) |
| Owner | Platform Engineering — Social/Engagement |

## 1. Purpose

This document specifies the complete badge framework for the Illumin360 platform. Badges are visible on public profile cards, in shortlist candidate cards (where appropriate), and serve as engagement incentives, trust signals, and recognition of platform status.

## 2. Badge catalogue

### 2.1 Candidate badges (`candidate_badges.badge_type`)

| Badge type | Display name | Award trigger | Revocable? |
|---|---|---|---|
| profile_complete | Profile Complete | profile_complete_pct reaches 100% | Auto if % drops |
| top_candidate | Top Candidate | Appeared in 5+ shortlists in last 90 days | Auto when rolling window expires |
| verified_professional | Verified Professional | Manual verification by admin (qualification check) | Admin |
| active_talent | Active Talent | Logged in 14+ days in last 30 days | Auto |
| graduate_ready | Graduate Ready | Student within 90 days of graduation with profile_complete_pct ≥ 80% | Auto on transition |
| skill_champion | Skill Champion | 10+ skills listed with advanced or expert proficiency | Auto |
| early_adopter | Early Adopter | Registered within first 30 days of platform launch | Permanent |
| cv_star | CV Star | CV uploaded and processed; recent upload (within 60 days) | Auto |
| graduate_spotlight | Graduate Spotlight | Featured in monthly Graduate Spotlight (Section 21 F5) | Permanent |
| top_referrer | Top Referrer | 5+ successful paid referrals | Permanent |
| **illumin360_founder** | **Illumin360 Founder** | First 300 job seekers (per ILLM-03-011) | Permanent — super-admin revocation only |
| **verified_student** | **Verified Student** | student_verifications.status changes to `verified` (per Section 26.1) | Auto on revocation/expiry |

### 2.2 Employer badges (`employer_badges.badge_type`)

| Badge type | Display name | Award criteria | Revocable? |
|---|---|---|---|
| **founding_partner** | **Illumin360 Founding Partner** | First 50 employers (per ILLM-03-011) | Permanent — super-admin only |
| **compliant_recruiter** | **Compliant Recruiter** | 10+ requests with declaration confirmed AND zero compliance justifications rejected AND no badge revocation in past 90 days | Monthly reassessment |
| active_employer | Active Employer | 1+ recruitment requests in last 90 days | Auto |
| university_partner | University Partner | Identified as a verified university partner | Admin |

### 2.3 Section 26 detail — Verified Student

| Attribute | Value |
|---|---|
| badge_type | `verified_student` |
| Award trigger | DB trigger `trg_award_verified_student_badge` fires when `student_verifications.verification_status` becomes `verified` |
| Display | Public profile card, social profile, shortlist candidate card — purple verification checkmark next to name |
| Auto-removal | Triggered when `verification_status` becomes `rejected` or `expired` (e.g., graduation grace period elapsed without paid upgrade) |
| Stored in | `candidate_badges` |

### 2.4 Section 26 detail — Compliant Recruiter

| Attribute | Value |
|---|---|
| badge_type | `compliant_recruiter` |
| Award criteria | All three must hold: (a) ≥ 10 recruitment requests with declaration D-03 confirmed, (b) 0 compliance justifications rejected by admin, (c) no badge revocation in past 90 days |
| Assessment | Monthly cron job `assess_compliant_recruiter_badges()` runs first day of each month at 02:00 WAT |
| Display | Employer profile page, employer's recruitment request cards (visible to job seekers when public-listing features arrive) |
| Revocation | Admin may revoke with documented reason. Reinstated automatically when criteria met again after a 90-day clean period. |
| Audit | Every award, revocation, and reinstatement logged |

## 3. Data model

### 3.1 `candidate_badges` (existing — extended in v2.0 refresh)

| Column | Type | Description |
|---|---|---|
| id | UUID PK | |
| job_seeker_id | UUID FK | |
| badge_type | ENUM | See §2.1 |
| earned_at | TIMESTAMPTZ | |
| revoked_at | TIMESTAMPTZ NULL | |
| revoked_reason | TEXT NULL | |
| is_permanent | BOOLEAN DEFAULT false | Founder, early_adopter, graduate_spotlight, top_referrer — set true |
| is_displayed | BOOLEAN DEFAULT true | Candidate may hide individual badges |
| metadata | JSONB | e.g., founder_number for illumin360_founder, spotlight month/year for graduate_spotlight |

Constraint: `UNIQUE(job_seeker_id, badge_type) WHERE revoked_at IS NULL` — one active instance of each badge type per candidate.

### 3.2 `employer_badges` (new in v2.0)

| Column | Type | Description |
|---|---|---|
| id | UUID PK | |
| employer_id | UUID FK | |
| badge_type | ENUM | See §2.2 |
| earned_at | TIMESTAMPTZ | |
| is_displayed | BOOLEAN DEFAULT true | |
| is_permanent | BOOLEAN DEFAULT false | founding_partner set true |
| revoked_at | TIMESTAMPTZ NULL | |
| revoked_by | UUID NULL | Admin UUID for revocation |
| revoked_reason | TEXT NULL | |
| metadata | JSONB | e.g., founder_number for founding_partner |

Constraint: `UNIQUE(employer_id, badge_type) WHERE revoked_at IS NULL`.

## 4. Award triggers

| Mechanism | Used by |
|---|---|
| Database trigger | `trg_award_verified_student_badge` on student_verifications. Fires INSERT into candidate_badges. |
| Application event handler | profile_complete reaching 100%, CV upload processed, login activity, skills changes |
| Scheduled cron — monthly | `assess_compliant_recruiter_badges()` evaluates §2.4 criteria for each employer |
| Scheduled cron — daily 06:00 WAT | Auto-removal: top_candidate window expiry, cv_star recency, active_talent recency, graduate_ready transitions |
| Manual admin grant | Verified Professional, University Partner, Graduate Spotlight |
| Registration-time | illumin360_founder, founding_partner (per ILLM-03-011) |

## 5. Display rules

### 5.1 Public profile card (`illumin360.com/p/[username]`)

Up to 6 badges are shown, ordered by `is_permanent` then `earned_at` descending. Candidate can hide individual badges via `is_displayed = false`.

### 5.2 Shortlist candidate card (in unlocked reports)

A limited subset appear next to the candidate name, prioritised:
1. `illumin360_founder` if present (always shown)
2. `verified_student` if present (always shown — high trust signal)
3. `verified_professional` if present
4. `top_candidate` if present
5. Up to one additional badge

Per Section 31 branding: badge names are Illumin360-branded — no "AI" or third-party references in badge text or imagery.

### 5.3 Anonymous preview (pre-payment)

Only `illumin360_founder` and `verified_student` badges appear on Candidate A/B/C/D cards — they are status indicators that influence employer payment decisions without revealing identity. Other badges remain hidden until the report is unlocked.

### 5.4 Employer profile page

Shows all employer badges in `is_displayed = true` state, ordered with `is_permanent` first. `founding_partner` and `compliant_recruiter` are prominent.

## 6. Cron jobs

### 6.1 `assess_compliant_recruiter_badges()`

```
For each employer:
  let request_count = COUNT(recruitment_requests WHERE declaration_confirmed = true)
  let rejected_count = COUNT(compliance_justifications WHERE rejected_by_admin = true
                              AND employer_id = e.id)
  let recent_revocation = EXISTS(employer_badges WHERE
                                  employer_id = e.id
                                  AND badge_type = 'compliant_recruiter'
                                  AND revoked_at > now() - INTERVAL '90 days')

  IF request_count >= 10 AND rejected_count = 0 AND NOT recent_revocation THEN
    IF NO active compliant_recruiter badge THEN
      INSERT INTO employer_badges (...)
  ELSE
    IF active compliant_recruiter badge THEN
      Skip — admin-only revocation
```

Awards happen automatically; revocations are admin-only. Logs all awards.

### 6.2 Daily badge maintenance (06:00 WAT)

Recomputes the volatile candidate badges:
- `cv_star`: requires CV uploaded in last 60 days
- `active_talent`: requires logins on 14+ days in last 30 days
- `top_candidate`: rolling 90-day window of shortlist appearances
- `graduate_ready`: based on expected_graduation_date

Adds or removes the badge as needed, logging every state change.

## 7. API endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | /me/badges | Logged-in user's badges |
| PUT | /me/badges/:id | Update `is_displayed` (show/hide) |
| GET | /public/p/:username/badges | Public-visible badges for a candidate's public profile card |
| GET | /admin/badges/employer-compliance | Compliant Recruiter assessment status across employers |
| POST | /admin/badges/grant | Manual admin grant |
| POST | /admin/badges/revoke | Admin revoke (employer badges or non-permanent candidate badges) |

## 8. Notifications

| Event | Notification |
|---|---|
| First badge ever earned | "You earned your first Illumin360 badge!" |
| Permanent badge earned (founder, early_adopter, spotlight, top_referrer) | "You earned the {badge name} — this is permanent." |
| Compliant Recruiter badge earned | Employer notified — "Your account has been recognised as a Compliant Recruiter." |
| Compliant Recruiter revoked | Employer notified with reason. |

## 9. Acceptance criteria

1. Verified Student badge is awarded automatically by trigger on `student_verifications.status = verified` and removed automatically on `rejected` or `expired`.
2. Compliant Recruiter monthly cron correctly applies the three criteria and produces an audit log entry for each award.
3. Compliant Recruiter cannot be re-awarded for 90 days after revocation.
4. `illumin360_founder` and `founding_partner` are permanent and cannot be revoked by standard admin actions.
5. Public profile card displays at most 6 badges, candidate-hidden badges absent.
6. Shortlist report displays the §5.2 priority subset only.
7. Anonymous preview displays only `illumin360_founder` and `verified_student`.
8. Badge names and imagery contain no "AI" or third-party provider references.

## 10. Cross-references

| Document | Section |
|---|---|
| Spec v3.6 | Section 21 F4, Section 26 (canonical), Section 30 (Founder badges) |
| Founder Programme Design (ILLM-03-011) | Founder badge grant logic |
| Database Design (ILLM-03-004 v2.0) | candidate_badges, employer_badges |
| API Design (ILLM-03-005 v2.0) | Badge endpoints |
| Social Features Design (ILLM-03-020) | Public profile card |
| Branding Policy (ILLM-03-012) | Badge name compliance |

## 11. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. |
