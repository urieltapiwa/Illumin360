# Social and Community Features — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 Social Features — Detailed Design |
| Document ID | ILLM-03-020_Social_Features_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft |
| Source authority | Section 21, Illumin360 Complete Technical Specification v3.6 |
| Phase | F1–F5, F7 in Phase 5; F6, F8 in Phase 6 |
| Owner | Platform Engineering — Social/Community |

## 1. Purpose

This document specifies the eight social and community features that transform Illumin360 from a transactional shortlisting tool into a platform with daily engagement and organic social sharing.

| Feature | Description | Phase |
|---|---|---|
| F1 | Shareable public profile card | 5 |
| F2 | Namibia talent demand feed | 5 |
| F3 | Career Insights blog | 5 |
| F4 | Skill badges and achievements (covered in ILLM-03-019) | 5 |
| F5 | Graduate Spotlight | 5 |
| F6 | Employer reviews | 6 |
| F7 | Referral programme | 5 |
| F8 | Namibia Talent Report | 6 |

## 2. F1 — Shareable public profile card

A public, indexable, brand-styled profile page at `illumin360.com/p/[username]`.

| Element | Specification |
|---|---|
| URL pattern | `illumin360.com/p/[username]` — username is candidate-chosen, unique, slug-safe |
| Content | Name (or initials if anonymous), city, primary qualification, top skills, badges, optional photo (per ILLM-03-017 §4.3 opt-in), optional bio |
| Download as PNG | Generated server-side from the same template — branded card downloadable for social sharing |
| Open Graph metadata | `og:title`, `og:description`, `og:image` (the PNG card), `og:type=profile` for rich previews on LinkedIn, X, Facebook, WhatsApp |
| Visibility | Public by default; candidate can switch profile to private |
| Branding | Illumin360-branded — no AI or third-party references |
| Indexing | `robots: index, follow` for public profiles; switched to `noindex` when set private |

API endpoints: `GET /public/p/:username` (rendered HTML), `GET /public/p/:username/card.png` (PNG download), `PUT /me/profile/visibility` (candidate toggles public/private).

## 3. F2 — Namibia talent demand feed

Anonymised weekly signal of employer demand on the platform — by role, city, skill, qualification — displayed on the public homepage.

| Element | Specification |
|---|---|
| Data source | Aggregated `recruitment_requests` from past 7 days |
| Aggregation | Group by (role_category, city), (skill, count), (qualification_level, count) — count = number of requests |
| Privacy | Below a minimum-count threshold (default 3), the segment is suppressed to prevent re-identification |
| Refresh | Daily cron at 04:00 WAT computes the next day's feed and caches it |
| Branding | "What employers are searching for this week" — plain English, no AI framing |
| Display | Cards on the public homepage. Mobile-friendly. Filterable by city. |

New table — `demand_feed_cache`:

| Column | Description |
|---|---|
| id | UUID PK |
| signal_type | ENUM — `role_x_city`, `top_skill`, `qualification_level` |
| label | Human-readable label |
| count | Aggregate count |
| city | NULL for non-city signals |
| week_starting | Monday of the week the data represents |
| is_suppressed | true when count < threshold |
| generated_at | TIMESTAMPTZ |

## 4. F3 — Career Insights blog

SEO-optimised, socially shareable articles on Namibian career advice and market data.

| Element | Specification |
|---|---|
| Authorship | Illumin team or invited contributors — bylined as "Illumin Insights" |
| URL pattern | `illumin360.com/insights/[slug]` |
| Editorial | Published from admin console; markdown editor with image upload |
| Frequency target | Two articles per month |
| Categories | Job Search, Hiring, Industry Trends, Student Pathways, Compliance |
| Tags | Free-form, slug-safe |
| SEO | Per-article meta description, Open Graph, structured data (Article schema.org) |
| Sharing | Native share buttons (X, LinkedIn, Facebook, WhatsApp, copy link) |
| Branding | Illumin360 voice. Articles may reference platform capabilities ("Illumin360 matching looks at...") but never "AI" or "Claude" |

New table — `insights`:

| Column | Description |
|---|---|
| id | UUID PK |
| slug | Unique, URL-safe |
| title, body_md | Markdown source |
| category, tags | Filterable |
| author_name | Byline |
| cover_image_url | Header image |
| meta_description, og_image_url | SEO |
| published_at, view_count | Lifecycle |
| status | `draft`, `published`, `archived` |

API: `GET /public/insights`, `GET /public/insights/:slug`, `POST /admin/insights` (admin only).

## 5. F5 — Graduate Spotlight

Monthly feature of five outstanding student profiles on the website and Illumin social channels.

| Element | Specification |
|---|---|
| Selection | Manual admin selection from the student pool — high profile completion, strong academic indicators, consent confirmed |
| Display | Monthly themed page at `illumin360.com/spotlight/[month-year]` |
| Per-student content | Photo (with explicit consent), institution, programme, GPA range, quote, skills snapshot |
| Awarded badge | `graduate_spotlight` (permanent badge per ILLM-03-019) |
| Consent | Each featured student confirms in writing via in-app dialog. Withdrawal possible at any time — page removes student. |
| Social sharing | Pre-formatted social media graphics per student, available for download |

New table — `spotlight_features`:

| Column | Description |
|---|---|
| id | UUID PK |
| student_id | FK |
| month, year | Feature period |
| quote | Up to 280 chars |
| photo_url | Subject to consent |
| consent_confirmed | BOOLEAN — must be true before publish |
| consent_confirmed_at, consent_revoked_at | Audit |
| published_at, removed_at | Lifecycle |

## 6. F6 — Employer reviews (Phase 6)

Anonymous star ratings from shortlisted candidates regarding their experience with an employer (responsiveness, interview process, professionalism).

| Element | Specification |
|---|---|
| Who can rate | Job seekers whose profile has been unlocked by the employer, after a 30-day window |
| Anonymity | Ratings are aggregated and never traced back to a specific candidate publicly. The platform records `job_seeker_id` internally for abuse prevention. |
| Aggregation | Mean across all ratings for an employer, with minimum 5 ratings required before any aggregate is publicly displayed |
| Top Employer badge | Employers maintaining ≥ 4.0 average across ≥ 20 ratings receive a `top_employer` badge (added to employer_badges) |
| Rating dimensions | Communication quality (1–5), Interview process (1–5), Professional conduct (1–5), Would recommend to a friend (Y/N) |
| Moderation | Free-text comments moderated by admin before publication |

New table — `employer_reviews`:

| Column | Description |
|---|---|
| id | UUID PK |
| employer_id | FK |
| job_seeker_id | FK — kept internal, not exposed publicly |
| communication, interview_process, conduct | INTEGER 1–5 |
| would_recommend | BOOLEAN |
| comment | TEXT NULL — moderated |
| moderated_at | TIMESTAMPTZ NULL |
| moderation_status | `pending`, `approved`, `rejected` |
| created_at | TIMESTAMPTZ |

## 7. F7 — Referral programme

Subscribers earn one free month per successful paid referral, no cap.

| Element | Specification |
|---|---|
| Referral link | `illumin360.com/r/[code]` — unique 8-char code per referrer |
| Tracking | Click stored against the referrer's account; cookie persists for 30 days |
| Reward trigger | Referee completes paid subscription. Reward = one free month added to referrer's subscription end_date. |
| Self-referral prevention | Referee account must use a different IP, email domain, and not have shared payment method with referrer |
| Cap | No upper limit on referrals or rewards earned |

New table — `referrals`:

| Column | Description |
|---|---|
| id | UUID PK |
| referrer_id | FK to users |
| referred_user_id | FK to users — NULL until conversion |
| referral_code | UNIQUE |
| status | `pending`, `referred_registered`, `converted_paid`, `reward_applied` |
| converted_at | TIMESTAMPTZ NULL |
| reward_applied_at | TIMESTAMPTZ NULL |
| reward_subscription_extension_days | INTEGER (default 30) |

## 8. F8 — Namibia Talent Report (Phase 6)

Annual free downloadable PDF report compiled from anonymised platform data — used for lead generation and brand authority.

| Element | Specification |
|---|---|
| Source | Aggregated platform data: top roles, top cities, qualification distribution, employer demand trends, candidate supply trends, graduate outcomes |
| Privacy | All data anonymised and aggregated. Suppression thresholds (per F2) apply. |
| Format | PDF (~30–50 pages) generated annually |
| Gating | Email-gated — visitor provides email to download. Email stored in marketing list with explicit opt-in. |
| Distribution | Public page on illumin360.com, social campaign on release, optional press release |

Implementation: a Python report-generation job pulls aggregates and renders via the existing WeasyPrint pipeline. No new tables beyond `report_downloads(email, downloaded_at, ip)` for lead-list management.

## 9. Branding compliance

All eight features per Section 31 branding policy:
- Public profile card uses "Illumin360 matching" not "AI matching"
- Demand feed reads "What employers are searching for" not "AI-detected demand signals"
- Career Insights articles do not name third-party providers
- Graduate Spotlight badge name is "Graduate Spotlight" — neutral
- Employer reviews UI uses plain English
- Referral programme uses plain English
- Talent Report cover and body do not name underlying technology

## 10. Acceptance criteria

1. Public profile card renders correctly with consented photo, badges, skills, and Open Graph metadata.
2. PNG download generates a brand-styled card with the same data as the HTML version.
3. Demand feed suppresses segments below the threshold and updates daily.
4. Career Insights articles publish with correct SEO metadata and social sharing previews.
5. Graduate Spotlight respects per-student consent and withdraws content immediately on consent revocation.
6. Employer reviews aggregate publicly only after ≥ 5 ratings; comments require admin moderation.
7. Referrals correctly attribute and apply rewards; self-referral attempts are blocked.
8. Talent Report PDF generates with no PII and email-gates the download with explicit opt-in.
9. No client-facing copy references AI or third-party providers.

## 11. Cross-references

| Document | Section |
|---|---|
| Spec v3.6 | Section 21 (canonical), Section 26 (badges) |
| Social Badges Design (ILLM-03-019) | F4 badges plus graduate_spotlight and top_referrer |
| Asset Management Design (ILLM-03-017) | Photo handling for public profile card |
| Branding Policy (ILLM-03-012) | Language compliance |
| Database Design (ILLM-03-004 v2.0) | New tables — demand_feed_cache, insights, spotlight_features, referrals, employer_reviews |
| API Design (ILLM-03-005 v2.0) | Social endpoints |

## 12. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. |
