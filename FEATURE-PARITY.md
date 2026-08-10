# Feature Parity & Roadmap

Cross-reference of Illumin360 against 10 comparable open-source systems (ATS, HRMS-with-recruitment,
job boards, matching engines), and the working checklist for closing the gaps.

**Reference systems:** OpenCATS, SpotAxis, EazyRecruit, Horilla, OrangeHRM, Frappe HR, OpenJobs,
Jobberbase, GitJobs, TalentMatch.

**Legend:** ✅ have · 🟡 partial · ⬜ missing. Checkboxes track build progress; tick `[x]` when done and
flip the status to ✅ (add the commit/PR ref).

---

## A. Candidate / talent management
| Feature | Status | Notes |
|---|---|---|
| Candidate & talent profiles (student/professional) | ✅ | Candidates + Students + Professionals services |
| Availability status | ✅ | `SetAvailability` on students & professionals |
| Skills & proficiency | 🟡 | Seeded skill rows; not user-editable |
| Resume/CV upload & storage | ✅ | MinIO-backed upload/download for professionals & students (self-service `/me/cv`) and candidates (per-id, admin) via `Illumin360.Storage` |
| Resume parsing (skills/experience extraction) | 🟡 | Shared `Illumin360.Resume` extracts CV text (PDF/DOCX/TXT) + detects skills; **professionals & students** `/me/cv/apply-skills` auto-add new skills to the profile ("Scan CV & add skills" UI). Candidates + experience/education extraction pending |
| Candidate search (boolean / faceted) | 🟡 | City ILIKE filter only |
| Recruiter notes / private activity log | 🟡 | Read-only activity feed; no recruiter notes |
| Tags / labels | ⬜ | |
| Skill endorsements / references | ⬜ | |
| Duplicate detection | ⬜ | |

- [x] Shared object-storage building block (`Illumin360.Storage`) + Professionals CV upload/download → MinIO (verified end-to-end with a Testcontainers MinIO roundtrip)
- [x] Extend CV upload to students (self-service `/me/cv`, UI + MinIO integration test) & candidates (per-id `/{id}/cv`, admin-gated)
- [x] Resume parsing — shared `Illumin360.Resume` (PdfPig + OpenXml text extraction, deterministic skill detection); `POST /me/cv/apply-skills` **auto-adds** newly detected skills to the Professionals profile (reflected live in the skills panel). Extending to students/candidates + parsing experience/education = follow-up
- [ ] Editable skills with proficiency
- [ ] Faceted candidate search (skills, city, availability)
- [ ] Recruiter notes + tags on a candidate

## B. Jobs / recruitment requisitions
| Feature | Status | Notes |
|---|---|---|
| Post recruitment request | ✅ | `POST /v1/recruitment/requests` |
| List with filters + paging | ✅ | city/status/page |
| Job detail | ✅ | `GET /requests/{id}` |
| Salary range / remote flag / category tags on a job | 🟡 | positions/city/status only |
| Public careers site (SEO job pages) | ⬜ | Marketplace panel is in-app only |
| Job approval workflow | ⬜ | |
| Job templates | ⬜ | |

- [ ] Extend requisition: salary range, employment type, remote flag, tags
- [ ] Public careers/job-listing pages (SSR/SEO)
- [ ] Requisition approval workflow

## C. Applications / pipeline (ATS core)
| Feature | Status | Notes |
|---|---|---|
| Apply to role | ✅ | marketplace apply + student/prof match apply |
| Applications-per-request listing | ✅ | `GET /requests/{id}/applications` |
| Pipeline stages (applied→reviewed→shortlist→interview→hire) | ✅ | Recruiter transition endpoints advance/reject with terminal-decision guards (409) |
| Advance / reject application (with reason) | 🟡 | Advance/reject endpoints (admin-gated); a free-text reason needs a new column on the externally-seeded `applications` table (pending) |
| Kanban pipeline board (per requisition) | ✅ | Admin-portal "Application pipeline" board — role selector + stage columns (applied→…→hired/rejected) with advance/reject. Drag-drop is a polish follow-up |
| Bulk actions | ⬜ | |
| Application status visible to applicant | ✅ | "My applications" live status timeline (`GET /recruitment/talents/{id}/applications`) on the professional portal |

- [x] Application stage-transition endpoints — `POST /v1/recruitment/applications/{id}/advance|reject` (admin-gated), domain stage machine (applied→reviewed→shortlisted→hired) with terminal-conflict guards. Free-text reject reason pending (needs a column on the externally-seeded `applications` table)
- [x] Recruiter pipeline board — Admin-portal kanban per requisition (role selector + stage columns, advance/reject on cards, live match %). Drag-drop = polish follow-up
- [x] Applicant-facing application status timeline — "My applications" panel on the professional portal, live status per applied role (`GET /recruitment/talents/{id}/applications`)

## D. Matching / sourcing
| Feature | Status | Notes |
|---|---|---|
| Match score candidate↔role | ✅ | Real engine (`Illumin360.Matching`) computes professional match scores from city + role + skills |
| Real matching engine (skills/location weighting) | ✅ | Shared weighted engine: professional & student matches, marketplace open-role ranking, and employer top-candidates (`GET /candidates/top`) |
| Personalized recommendations | ✅ | Professional matches and marketplace open roles both ranked by engine score (`/me/role-scores`) |
| Saved searches | ✅ | Talent saved searches (create/list/delete + run-results) — Recruitment `saved_searches` table + professional-portal panel |
| Job alerts / email digests | ✅ | Per-search alerts opt-in + a scheduled `JobAlertScheduler` that runs alert-enabled searches → `JobAlertDigest` event → Notifications worker emails the matches |
| Talent pools / shortlists | ✅ | Named recruiter pools (`/v1/candidates/pools`) — create + add/remove candidates (dedup) + enriched members list |

- [x] Matching engine (weighted city + role + skills) producing real scores — shared `Illumin360.Matching`, applied to **professional & student** matches (ranked by score) and the professional marketplace panel
- [x] "Recommended roles for you" — marketplace open roles ranked per professional (`POST /me/role-scores`, match % shown/sorted) — and the employer flip side, "top candidates for a role" (`GET /v1/candidates/top?title=&city=`)
- [x] Saved searches — talent CRUD + run-results (`/v1/recruitment/saved-searches`), professional-portal panel, plus a per-search **job-alerts opt-in** toggle
- [x] Scheduled alert-digest sender — `JobAlertScheduler` background service runs alert-enabled searches on an interval, publishes `JobAlertDigest` (outbox) → Notifications worker emails the matching roles
- [x] Shortlists / talent pools — named recruiter pools with create + add/remove candidates (dedup guard) + members listing (`/v1/candidates/pools`), admin-gated writes. Recruiter UI is a follow-up

## E. Interviews & scheduling
| Feature | Status | Notes |
|---|---|---|
| Interview scheduling | ✅ | Schedule/list/cancel interviews per application (`interviews` table + admin-gated endpoints) |
| Calendar integration (ICS/Google) | 🟡 | `.ics` invite download (`/interviews/{id}/ics`, importable to Google/Outlook); no direct calendar-API sync |
| Interview scorecards / feedback | ✅ | Rating (1–5) + comment completes an interview |
| Panel interviews | ⬜ | Single interviewer only (no attendees list yet) |

- [x] Schedule interview (slot, location/mode, `.ics` invite) — schedule/list/cancel + `/interviews/{id}/ics`. Multi-attendee panels are a follow-up
- [x] Interview scorecard + feedback capture — rating (1–5) + comment completes the interview

## F. Offers & onboarding
| Feature | Status | Notes |
|---|---|---|
| Offer management | ⬜ | |
| Offer letter / e-sign | ⬜ | |
| Onboarding checklist | ⬜ | Horilla/Frappe HR have this |

- [ ] Offer create/accept/decline workflow
- [ ] Onboarding checklist on hire

## G. Communication & notifications
| Feature | Status | Notes |
|---|---|---|
| Event-driven notifications (outbox) | ✅ | MassTransit outbox + Notifications worker |
| Transactional email (templated) | ✅ | Shared `Illumin360.Email` (MailKit/SMTP → Mailpit) + templates; Notifications worker emails on registration, application received, and application status change (recruitment events via the outbox) |
| In-app notification center | ✅ | Professional in-app notifications (list / mark-read / mark-all) fed by recruitment events (status change, job alerts); portal panel with unread count |
| In-app messaging (candidate↔employer) | ⬜ | |
| Bulk email / campaigns | ⬜ | |

- [x] Email infrastructure — shared `Illumin360.Email` (MailKit SMTP → Mailpit) + templates; Notifications worker sends a **welcome email on registration** (verified end-to-end with a Testcontainers Mailpit)
- [x] Templated emails on application received / status change — Recruitment publishes `ApplicationSubmitted` / `ApplicationStatusChanged` via the outbox; Notifications worker consumers send the emails
- [x] In-app notification center — Professionals consume recruitment events (status change, job alerts) into a `professional_notifications` store; `/me/notifications` list + mark-read/mark-all + portal panel with unread count
- [ ] Direct messaging between employer and candidate

## H. Employer / recruiter tooling
| Feature | Status | Notes |
|---|---|---|
| Employer self-registration (identity + role) | ✅ | via BFF `/register` |
| Employer/company profile service | ✅ | New `Illumin360.Employers` microservice — company profile get/register/update (`/v1/employers`), DB-per-service + migration + seed, gateway route |
| Employer portal UI | ✅ | `?portal=employer` page — company profile view + inline edit (industry/city/website/about) against `/api/employers/me`, plus a "top candidates for a role" panel wired to `/api/candidates/top` |
| Multi-user employer teams + roles | ✅ | `employer_team_members` table + `/v1/employers/me/team` list/invite/change-role/remove (owner/recruiter/viewer), "at least one owner" invariant, unique email per employer; team panel in the employer portal |
| Recruiter CRM (clients/contacts) | ⬜ | OpenCATS has this |
| Branded careers page | ⬜ | |

- [x] Employers service — new `Illumin360.Employers` microservice (Domain/Application/Infrastructure/Api) with company profile get/register/update, DB-per-service (migrate + seed), gateway route `/api/employers/**`, unit + Testcontainers integration tests
- [x] Employers deploy — chiseled non-root Dockerfile + `employers-api` service in `docker-compose.apps.yml` (port 5206, gateway dependency; `illumin360_employers` DB already provisioned by the init script)
- [x] Employer portal UI — `?portal=employer` company-profile page: live profile view + inline edit (industry/city/website/about; company name fixed) via `PUT /api/employers/me`, and a "top candidates for a role" ranking panel (`GET /api/candidates/top`). Read-only snapshot fallback when the API is offline. Company **members/teams** are the follow-up
- [x] Employer team roles (owner/recruiter/viewer) — `TeamMember` aggregate + `employer_team_members` table (unique email per employer), `/v1/employers/me/team` list/invite/change-role/remove (writes admin-gated), "at least one owner" invariant guarding demotion & removal (409), seeded founding owner, unit + Testcontainers integration tests, and a team-management panel in the employer portal

## I. Admin & governance
| Feature | Status | Notes |
|---|---|---|
| Verification queue (approve/reject) | ✅ | Admin service |
| Support tickets (assign/resolve) | ✅ | Backend + Admin portal panel live (assign + resolve) |
| User account management (suspend/activate) | ✅ | Backend + Admin portal panel live (suspend/activate) |
| Service-layer RBAC | ✅ | `Illumin360.Security` |
| Audit trail (viewable) | 🟡 | Outbox events exist; no audit UI |
| GDPR data export / delete | ⬜ | |

- [x] Admin portal panels for tickets + accounts (wire to existing APIs) — panels were already wired; completed by adding the ticket **Assign** action + assignee display (`Admin.tsx`)
- [ ] Viewable audit trail
- [ ] GDPR export / erase-me

## J. Analytics & reporting
| Feature | Status | Notes |
|---|---|---|
| Recruitment stats / funnel / dashboards | ✅ | `/v1/recruitment/stats` + portal charts |
| Time-to-hire / source metrics | 🟡 | Partial stats |
| Custom reports / CSV-PDF export | ⬜ | |
| Diversity / EEO reporting | ⬜ | |

- [ ] Export dashboards/reports to CSV/PDF
- [ ] Time-to-hire and source-of-hire metrics

## K. Platform / cross-cutting
| Feature | Status | Notes |
|---|---|---|
| OIDC auth + BFF + RBAC | ✅ | Keycloak + Business BFF |
| Self-registration + email verification | ✅ | 3 user types, compensating profile creation |
| i18n (EN + Afrikaans) | ✅ | all five portals |
| REST API + OpenAPI | ✅ | per service |
| Object storage (MinIO) | ✅ | `Illumin360.Storage` building block; CV uploads use it |
| Integration test coverage | ✅ | Testcontainers smoke tests + TestSupport |
| Mobile app | ⬜ | OrangeHRM has one |

---

### Progress
- Total build items: 32
- Done: 21
- In progress: 0

**Changelog of ticks**
- Admin portal tickets + accounts panels — completed the ticket Assign action (2026-08-10).
- Shared object storage + Professionals CV upload/download (MinIO), end-to-end tested (2026-08-10).
- CV upload extended to Students (self-service + UI) and Candidates (per-id, admin), MinIO-tested (2026-08-10).
- Shared matching engine (`Illumin360.Matching`) — real weighted scores + ranking on professional matches (2026-08-10).
- Marketplace open roles ranked per professional (`/me/role-scores`), match % shown + sorted (2026-08-10).
- Matching engine extended to student dashboard matches (scored + ranked) (2026-08-10).
- Employer "top candidates for a role" ranking endpoint (`GET /candidates/top`) (2026-08-10).
- Resume parsing (`Illumin360.Resume`) — CV text extraction + skill detection, wired to Professionals `/me/cv/parse` + UI (2026-08-10).
- Auto-apply detected CV skills to the professional profile (`/me/cv/apply-skills`), reflected live (2026-08-10).
- Extended CV parse/apply-skills to students (same `/me/cv/apply-skills` + "Scan CV" UI) (2026-08-10).
- Application pipeline stage-transition endpoints (advance/reject) with a domain stage machine (2026-08-10).
- Applicant status timeline — "My applications" panel + `GET /recruitment/talents/{id}/applications` (2026-08-10).
- Email infrastructure (`Illumin360.Email`, MailKit→Mailpit) + welcome email on registration; Mailpit-verified (2026-08-10).
- Application-event emails — Recruitment publishes ApplicationSubmitted/StatusChanged (outbox) → worker sends templated emails (2026-08-10).
- Saved searches (CRUD + run-results) + per-search job-alerts opt-in; professional-portal panel (2026-08-10).
- Scheduled job-alert digest sender — JobAlertScheduler → JobAlertDigest event → worker emails matches (2026-08-10).
- Recruiter kanban pipeline board in the Admin portal (stage columns + advance/reject) (2026-08-10).
- Interviews & scheduling — schedule/feedback/cancel + .ics invite (`interviews` table) (2026-08-10).
- In-app notification center — Professionals consume recruitment events → notifications store + portal panel (2026-08-10).
- Shortlists / talent pools — named recruiter pools with add/remove candidates (Candidates service) (2026-08-10).
- New Illumin360.Employers microservice — company profile get/register/update + gateway route (2026-08-10).
- Employers service wired into docker-compose (Dockerfile + `employers-api` on 5206, gateway dependency) (2026-08-10).
- Employer portal UI — `?portal=employer` company-profile view + inline edit + top-candidates panel (2026-08-10).
- Employer team roles — `employer_team_members` + `/v1/employers/me/team` CRUD (owner/recruiter/viewer), last-owner invariant, portal team panel (2026-08-10).

_Update this file as items are ticked; link the commit/PR that delivered each._
