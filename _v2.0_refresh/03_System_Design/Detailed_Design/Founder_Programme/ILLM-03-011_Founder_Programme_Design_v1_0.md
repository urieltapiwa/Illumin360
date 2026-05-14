# Illumin360 Founder Programme — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 Founder Programme — Detailed Design |
| Document ID | ILLM-03-011_Founder_Programme_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft — for technical and commercial review |
| Source authority | Section 30, Illumin360 Complete Technical Specification v3.6 |
| Phase | Phase 1 (Core Talent Pool) — quota enforcement live from launch day 1 |
| Owner | Platform Architecture |

## 1. Purpose

This document specifies the design of the Illumin360 Founder Programme, the platform's go-to-market quota mechanism that grants permanent, non-expiring platform access to the first 300 job seekers and first 50 employers to register on the platform. It covers commercial intent, data model, registration-time enforcement, race-condition protection, badge issuance, admin override flow, and audit requirements.

## 2. Commercial intent and scope

The Founder Programme is the platform's launch-stage acquisition mechanism. Quotas close automatically once filled; there is no extension, no waitlist, and no public retroactive grant.

| Group | Quota | Permanent benefit | Recurring billing |
|---|---|---|---|
| Job seeker founders | First 300 to register | Discoverable in perpetuity, no renewal payment ever required, permanent Illumin360 Founder badge | None — never billed for subscription. Employer unlock fees apply to employer side only. |
| Employer founding partners | First 50 to register | Permanent employer account, no account expiry, permanent Illumin360 Founding Partner badge | Per-request billing applies as for all employers — NAD 1,725 standard, NAD 2,300 internal. Founders pay for reports they unlock. |

The rationale for the asymmetric billing treatment: candidate scarcity is the platform's bootstrap problem, so candidate Founders are subsidised; employer revenue depends on per-request usage, so employer Founders receive recognition status only, preserving the revenue motive to use the platform.

## 3. Lifecycle states

| State | Job seeker founder | Employer founding partner |
|---|---|---|
| Pre-claim | `is_founder = false`, slot available | `is_founder = false`, slot available |
| Granted at registration | Trigger sets `is_founder = true`, badge issued, founder_number assigned | Same — founding partner badge |
| Active | Profile/account active permanently — subscription expiry checks skipped | Account active permanently — no expiry job runs against it |
| Dormant | After 12 consecutive months of empty profile activity: re-engagement email sent. **Account is never deactivated by the system.** Only the Founder themselves may delete. | After 12 consecutive months without a recruitment request: re-engagement email sent. **Account is never deactivated by the system.** |
| Voluntary deletion | Founder requests account deletion. `is_founder` flag retained in `founder_registrations` for audit. Slot is **not** reissued. | Same. |
| Admin override grant | Admin can manually grant outside the quota with documented justification. Recorded with `granted_by` = admin UUID and reason. | Same. |

A Founder slot is consumed when granted and is not returned to the pool on deletion. This prevents quota gaming and gives Founder status its scarcity value.

## 4. Data model

### 4.1 New table — `founder_registrations`

| Column | Type | Constraints | Description |
|---|---|---|---|
| id | UUID | PK | Surrogate key |
| user_id | UUID | FK → users(id), UNIQUE | The Founder user (job seeker or employer). UNIQUE prevents double-grant. |
| user_type | ENUM | NOT NULL — values: `job_seeker`, `employer` | Quota domain |
| founder_number | INTEGER | NOT NULL, UNIQUE per user_type | Sequential 1..300 for job seekers, 1..50 for employers. Used for badge ordinal display. |
| granted_at | TIMESTAMPTZ | NOT NULL DEFAULT now() | Grant timestamp |
| granted_by | UUID | NULL = system grant; non-NULL = admin user UUID for override | Audit |
| grant_reason | TEXT | NULL for system grants; required for admin overrides | Audit |
| revoked_at | TIMESTAMPTZ | NULL unless revoked | Reserved — Founders cannot be revoked by standard admin; super-admin only |
| revoked_by | UUID | NULL unless revoked | Audit |
| revocation_reason | TEXT | NULL unless revoked | Audit |

Indexes: `(user_type, founder_number)`, `(user_id)`.

### 4.2 Existing-table additions

| Table | Column added | Type | Default | Purpose |
|---|---|---|---|---|
| job_seekers | is_founder | BOOLEAN | false | Quick lookup — bypasses subscription expiry checks |
| employers | is_founder | BOOLEAN | false | Quick lookup — bypasses account expiry checks |
| subscriptions | plan_type ENUM value | — | — | Add `founder_permanent` to plan_type enumeration |
| candidate_badges | badge_type ENUM value | — | — | Add `illumin360_founder` (permanent, non-revocable by standard admin) |
| employer_badges | badge_type ENUM value | — | — | Add `founding_partner` (permanent, non-revocable by standard admin) |

### 4.3 Pricing plan row

A new row in `pricing_plans`:

| plan_type | name | base_price | Behaviour |
|---|---|---|---|
| founder_permanent | Illumin360 Founder — Permanent Subscription | NAD 0.00 | No invoice ever generated. Reminder cron filters this plan out. |

## 5. Registration-time grant logic

Both grants run inside the user-registration database transaction, gated by `SELECT ... FOR UPDATE` to serialise concurrent registrations attempting to claim the same final slot.

### 5.1 Job seeker registration grant

```
BEGIN TRANSACTION;

-- 1. Lock the founder count row to prevent concurrent claims
SELECT COUNT(*) AS claimed
  FROM founder_registrations
 WHERE user_type = 'job_seeker'
   FOR UPDATE;

-- 2. If under quota, grant
IF claimed < 300 THEN
  INSERT INTO founder_registrations
    (user_id, user_type, founder_number, granted_at)
  VALUES
    (:new_user_id, 'job_seeker', claimed + 1, now());

  UPDATE job_seekers SET is_founder = TRUE WHERE id = :new_user_id;

  INSERT INTO subscriptions
    (user_id, plan_type, start_date, end_date, status)
  VALUES
    (:new_user_id, 'founder_permanent', now(), NULL, 'active');

  INSERT INTO candidate_badges
    (job_seeker_id, badge_type, earned_at, is_permanent)
  VALUES
    (:new_user_id, 'illumin360_founder', now(), TRUE);

  -- Log
  INSERT INTO audit_logs (...) VALUES (...);
END IF;

COMMIT;
```

The `FOR UPDATE` lock guarantees that two simultaneous registration transactions cannot both observe `claimed = 299` and both grant slot 300. Lock contention is acceptable: registrations are infrequent and Founder claims happen only during the launch window.

### 5.2 Employer registration grant

Identical logic against `user_type = 'employer'`, quota of 50, badge `founding_partner` written to `employer_badges`. Employer subscription_or_status is *not* created — employers are pay-per-request — but `employers.is_founder` is set.

### 5.3 Failure modes

| Failure | Behaviour |
|---|---|
| Concurrent claim — slot taken between SELECT and INSERT | Cannot happen — `FOR UPDATE` serialises |
| Transaction rolls back after grant insert | User registration fails entirely; founder slot not consumed; slot remains available |
| Quota already full | Registration proceeds normally with standard paid plan; no error, no message to user. Quota status only displayed in admin dashboard. |
| Duplicate user_id | UNIQUE constraint on `founder_registrations.user_id` blocks; transaction rolls back. Cannot grant Founder twice to one user. |

## 6. Admin override flow

A platform administrator may grant Founder status outside the standard registration path — for example, a high-profile early supporter who registered after the quota closed. The override is governed:

| Step | Behaviour |
|---|---|
| Admin opens `Admin → Users → [user] → Grant Founder` | Action visible only to users with role `admin` and capability `founder.override` |
| Admin enters a free-text justification (minimum 30 words) | Stored in `founder_registrations.grant_reason` |
| System assigns next available founder_number | Even if outside the quota — `founder_number` becomes `301+` for job seeker, `51+` for employer. Quota count for system grants is unaffected. |
| Audit log entry | `audit_logs` row with `event_type = 'founder_admin_override'`, full metadata |
| Sponsor notification (optional) | If `granted_by` is not the platform owner, the platform owner receives a notification. Configurable. |

Admin overrides never reissue an existing slot — they create supernumerary Founder records.

## 7. Revocation

Founders cannot be revoked by standard admins. Only super-admins (role `super_admin`) may revoke, and only with cause: confirmed fraud, terms-of-service violation, or written user request.

Revocation behaviour:
- `founder_registrations.revoked_at`, `revoked_by`, `revocation_reason` populated
- `is_founder` flag cleared on the user row
- Badge marked `is_revoked = true` on `candidate_badges` / `employer_badges`
- Subscription downgraded to a paid plan with a 30-day grace period for the candidate to renew, or marked inactive for the employer
- Audit log entry
- Revocation does **not** reissue the slot to the public quota

## 8. Admin dashboard

The admin dashboard surfaces Founder programme telemetry in real time.

| Widget | Display |
|---|---|
| Job seeker quota status | `247 of 300 Founder job seeker slots claimed. 53 remaining.` Progress bar. |
| Employer quota status | `38 of 50 Founding Partner employer slots claimed. 12 remaining.` Progress bar. |
| Founders list | Searchable table of all Founders with founder_number, registered date, last activity |
| Override action | Admin can navigate to a specific user and trigger the override (§6) |
| Dormancy report | Founders inactive 12+ months — for re-engagement email campaign |

When job seeker quota reaches 290 and employer quota reaches 45, an alert is sent to platform owner notifying the imminent quota close.

## 9. Public-facing communications

The Founder Programme is mentioned on the public homepage and in pricing pages during the launch window only. Once a quota fills:
- Public-facing copy referring to that programme is auto-hidden via feature flag `founder_<type>_programme_open`
- Founder badges remain visible on Founder public profile cards permanently — they are the only public ongoing reference to the programme after quota close

The Founder Programme is referenced in:
- Public homepage hero (`Be one of the first 300 job seekers...`)
- Pricing page sidebar
- Email campaign templates (`founder_welcome_jobseeker`, `founder_welcome_employer`)
- Sales talking points (per Sales Marketing deliverables)

Per Section 31 branding policy, **no mention of AI vendor, Claude, or any third-party technology is made** in any Founder Programme client-facing content.

## 10. API endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | /public/founder/status | None | Returns `{job_seeker_remaining, employer_remaining, total_quotas}` — used by public homepage badge and pricing page. Cached 60 seconds. |
| GET | /admin/founder/registrations | Admin | List all Founder records with filters |
| POST | /admin/founder/grant | Admin (capability `founder.override`) | Body `{user_id, reason}`. Performs admin override grant. |
| POST | /admin/founder/revoke | Super-admin only | Body `{founder_registration_id, reason}`. Revokes Founder status. |
| GET | /admin/founder/dormancy | Admin | Founders inactive 12+ months for re-engagement campaign |

## 11. Notifications

| Trigger | Recipient | Template |
|---|---|---|
| Job seeker Founder granted (system) | Job seeker | `founder_welcome_jobseeker` — explains permanent status, badge visibility, no renewal needed |
| Employer Founding Partner granted (system) | Employer | `founder_welcome_employer` — explains permanent account, badge, per-request billing applies |
| Admin override grant | Platform owner | Internal notification |
| Dormancy 12 months | Founder user | `founder_reengagement_<type>` — soft re-engagement, no threat of deactivation |
| Quota approaching close (290 / 45) | Platform owner | Internal alert |
| Quota closed | Platform owner | Internal alert. Public-facing copy switched off via feature flag automatically. |

## 12. Audit and compliance

Every Founder lifecycle event is written to `audit_logs` immutably with 7-year retention per Section 15.3.

| Event type | Logged fields |
|---|---|
| founder_granted_system | user_id, user_type, founder_number, granted_at, IP, registration metadata |
| founder_granted_admin_override | user_id, user_type, founder_number, granted_at, granted_by (admin UUID), grant_reason, IP |
| founder_revoked | founder_registration_id, revoked_by (super_admin UUID), revocation_reason, IP |
| founder_badge_displayed | First time a Founder badge is rendered to a public-facing viewer — for marketing reach measurement |

## 13. Acceptance criteria

1. Job seeker registration 1–300 receives Founder status automatically with no UI difference visible at registration time other than the welcome email and badge appearing on their profile after first login.
2. Employer registration 1–50 receives Founding Partner status equivalently.
3. Registration 301 (job seeker) and 51 (employer) proceeds with standard paid pricing — no error, no banner, no message about quota close.
4. Two simultaneous registration attempts both observing `claimed = 299` resolve to one grant and one standard registration — never two grants of slot 300. Tested under 50-concurrent-write load.
5. Founder profile/account never enters expiry state. Subscription expiry cron skips records where `plan_type = 'founder_permanent'`. Employer expiry cron skips records where `is_founder = true`.
6. Admin can grant Founder status manually via the override flow with mandatory justification.
7. Founder records are never destroyed on account deletion — `founder_registrations` retains the record with `user_id` retained for audit, and the slot is not reissued.
8. Public homepage and pricing page automatically reflect quota status within 60 seconds of the final slot being claimed.
9. All Founder lifecycle events are recorded in `audit_logs` and cannot be deleted by any UI action including admin actions.

## 14. Open questions

| # | Question | Action |
|---|---|---|
| 1 | Should the Founder badge appear in shortlist reports alongside the candidate's name, or only on the public profile card? | Product decision required before launch |
| 2 | Should there be a public Founder leaderboard (Founder #1 through #300, names with consent)? | Marketing decision — requires explicit consent step at registration |
| 3 | Dormancy grace — 12 months is current. Should re-engagement attempt be repeated annually or only once? | Marketing decision |
| 4 | Should employer Founding Partners receive any per-request pricing benefit (e.g. one free standard report at launch)? | Commercial decision — currently no, per Section 30.2 |

## 15. Cross-references

| Document | Section / location |
|---|---|
| Illumin360 Complete Technical Specification v3.6 | Section 30 (canonical), Section 13 (DB), Section 14 (API), Section 15 (compliance), Section 26 (badge framework) |
| Database Design (ILLM-03-004 v2.0) | Founder tables and columns added in v2.0 refresh |
| API Design (ILLM-03-005 v2.0) | Founder endpoints added in v2.0 refresh |
| Business Case (ILLM-01-001 v2.0) | GTM strategy section in v2.0 refresh |
| Phase 1 Incremental Delivery (ILLM-06-001 v2.0) | Quota enforcement live from Phase 1 |
| Social Badges Design (ILLM-03-019) | Badge framework foundation |

## 16. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. Created to address coverage gap identified in v2.0 SDLC refresh — Founder Programme had no owning detailed-design document prior to this. |
