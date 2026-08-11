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
| Skills & proficiency | ✅ | Professionals `/me/skills` add/update-level/remove (0–100 proficiency, dedup by name), editable skills panel (proficiency sliders + add/remove) on the professional portal |
| Resume/CV upload & storage | ✅ | MinIO-backed upload/download for professionals & students (self-service `/me/cv`) and candidates (per-id, admin) via `Illumin360.Storage` |
| Resume parsing (skills/experience extraction) | ✅ | Shared `Illumin360.Resume` — CV text extraction (PDF/DOCX/TXT), skill detection, and heuristic **experience/education** extraction (`ExperienceExtractor`); professional CV parse returns skills + experience + education; `/me/cv/apply-skills` auto-adds skills |
| Candidate search (boolean / faceted) | ✅ | `GET /v1/candidates/search` — city + availability + keyword (name/headline) + has-CV filters, paged, with facet counts (each facet excludes its own filter); admin-portal candidate-search panel |
| Recruiter notes / private activity log | ✅ | Private recruiter notes per candidate — `candidate_notes` table + `/v1/candidates/{id}/notes` list/add/delete (writes admin-gated); admin candidate-search notes panel |
| Tags / labels | ✅ | Candidate tags — `candidate_tags` table (unique per candidate, normalised) + `/v1/candidates/{id}/tags` list/add(idempotent)/remove; tag chips in the admin candidate-search panel |
| Skill endorsements / references | ✅ | `skill_endorsements` table + denormalised count on `professional_skills`; `POST/GET /v1/professionals/skills/{id}/endorsements` (endorse admin-gated, dedup per endorser, optional reference note); endorsement ★ count shown on the professional skills panel |
| Duplicate detection | ✅ | `GET /v1/candidates/duplicates` clusters candidates sharing a normalised name (optional same-city strictness); "Possible duplicates" panel in the admin portal |

- [x] Shared object-storage building block (`Illumin360.Storage`) + Professionals CV upload/download → MinIO (verified end-to-end with a Testcontainers MinIO roundtrip)
- [x] Extend CV upload to students (self-service `/me/cv`, UI + MinIO integration test) & candidates (per-id `/{id}/cv`, admin-gated)
- [x] Resume parsing — shared `Illumin360.Resume` (PdfPig + OpenXml text extraction, deterministic skill detection); `POST /me/cv/apply-skills` **auto-adds** newly detected skills to the Professionals profile (reflected live in the skills panel)
- [x] Experience/education extraction — `ExperienceExtractor` heuristically parses work-experience and education entries from CV text (section-heading detection + year/range lines → title/organization/period, section-boundary aware); surfaced in the professional CV-parse result (skills + experience + education); pure unit tests
- [x] Editable skills with proficiency — Professionals `POST/PUT/DELETE /v1/professionals/me/skills` (add with 0–100 level + dedup-by-name conflict, update level with range validation, remove), skill ids surfaced on the dashboard; professional-portal skills panel now has proficiency sliders + an add-skill row + remove; handler/domain unit tests
- [x] Faceted candidate search — `GET /v1/candidates/search` filtering on city, availability, keyword (name/headline ILIKE) and CV presence, paged with a total, returning facet counts for cities + availability (each facet excludes its own active filter); handler unit test + Testcontainers integration test; admin-portal search panel with clickable city facets. (Skill facets await structured candidate skills)
- [x] Recruiter notes + tags on a candidate — `CandidateNote`/`CandidateTag` aggregates + `candidate_notes`/`candidate_tags` tables (tags unique + normalised per candidate); `/v1/candidates/{id}/notes` (list/add/delete) and `/v1/candidates/{id}/tags` (list/add-idempotent/remove), writes admin-gated; unit + Testcontainers integration tests; expandable notes/tags panel per candidate in the admin search results
- [x] Skill endorsements / references — `SkillEndorsement` aggregate + `skill_endorsements` table (dedup per endorser, optional reference note) with a denormalised `endorsements` count on `professional_skills`; `POST /v1/professionals/skills/{id}/endorsements` (admin-gated, 404/409 guards) + `GET` list; endorsement count surfaced on the professional skills panel; domain/handler unit tests
- [x] Duplicate detection — `GET /v1/candidates/duplicates` clusters the pool by normalised first+last name (optional `sameCityOnly` strictness), returning groups (size > 1) with their members; handler unit tests; "Possible duplicates" panel in the admin portal

## B. Jobs / recruitment requisitions
| Feature | Status | Notes |
|---|---|---|
| Post recruitment request | ✅ | `POST /v1/recruitment/requests` |
| List with filters + paging | ✅ | city/status/page |
| Job detail | ✅ | `GET /requests/{id}` |
| Salary range / remote flag / category tags on a job | ✅ | Service-owned `requisition_details` (+ `requisition_tags`) side-tables + `GET/PUT /v1/recruitment/requests/{id}/details` and tag add/remove; admin pipeline role-details editor (salary range, employment type, remote, tags) |
| Public careers site (SEO job pages) | ✅ | Server-rendered `/careers` listing + `/careers/{id}` detail with schema.org JobPosting JSON-LD (see branded careers page in H) |
| Job approval workflow | ✅ | `requisition_approvals` side-table + `GET` / `POST …/approval/{submit\|approve\|reject}` — draft→submitted→approved/rejected state machine (409 guards, resubmit after reject); admin pipeline approval controls |
| Job templates | ✅ | `job_templates` table + `GET/POST/DELETE /v1/recruitment/templates` and `POST …/{id}/use` (creates a requisition + enrichment + tags from a template); admin job-templates panel |

- [x] Extend requisition: salary range, employment type, remote flag, tags — service-owned `RequisitionDetail` (+ `RequisitionTag`) side-tables keyed 1:1 by the externally-seeded request id; `GET/PUT /v1/recruitment/requests/{id}/details` (upsert, salary-range + type validation) and `POST/DELETE …/tags/{label}` (idempotent); unit tests; admin pipeline role-details editor (salary min/max, employment type, remote toggle, tag chips)
- [x] Public careers/job-listing pages (SSR/SEO) — server-rendered `/careers` (open-role listing) + `/careers/{id}` (role detail) from the Recruitment service, with descriptive title/meta/Open Graph/canonical tags and schema.org JSON-LD (`ItemList` on the index, `JobPosting` on detail), HTML-escaped; exposed publicly through the gateway (`/careers/**` → `/v1/recruitment/careers/**`). Pure `CareersHtml` renderer unit-tested
- [x] Requisition approval workflow — `RequisitionApproval` aggregate + `requisition_approvals` side-table (unique per request); `GET /v1/recruitment/requests/{id}/approval` + `POST …/approval/submit|approve|reject` with a draft→submitted→approved/rejected state machine (409 guards, reason required on reject, resubmit after reject), writes admin-gated; unit tests; admin pipeline approval status + submit/approve/reject controls
- [x] Job templates — `JobTemplate` aggregate + `job_templates` table (unique name, tags stored joined); `GET/POST/DELETE /v1/recruitment/templates` (name-conflict 409, salary/type validation) + `POST …/{id}/use` which posts a new requisition and carries the template's enrichment (salary/type/remote) + tags onto it; unit tests; admin job-templates panel (list/create/delete)

## C. Applications / pipeline (ATS core)
| Feature | Status | Notes |
|---|---|---|
| Apply to role | ✅ | marketplace apply + student/prof match apply |
| Applications-per-request listing | ✅ | `GET /requests/{id}/applications` |
| Pipeline stages (applied→reviewed→shortlist→interview→hire) | ✅ | Recruiter transition endpoints advance/reject with terminal-decision guards (409) |
| Advance / reject application (with reason) | ✅ | Advance/reject endpoints (admin-gated); free-text reject reason stored in a service-owned `application_rejections` side-table (the seeded `applications` table isn't writable), surfaced on the application listing + kanban cards |
| Kanban pipeline board (per requisition) | ✅ | Admin-portal "Application pipeline" board — role selector + stage columns (applied→…→hired/rejected) with advance/reject **and drag-and-drop** (drag a card to a later stage → chained advances; drop on Rejected → reject; valid drop targets highlight) |
| Bulk actions | ✅ | `POST /v1/recruitment/applications/bulk` advances/rejects many applications at once (per-item results, batch cap, dedup); kanban card checkboxes + bulk action bar |
| Application status visible to applicant | ✅ | "My applications" live status timeline (`GET /recruitment/talents/{id}/applications`) on the professional portal |

- [x] Application stage-transition endpoints — `POST /v1/recruitment/applications/{id}/advance|reject` (admin-gated), domain stage machine (applied→reviewed→shortlisted→hired) with terminal-conflict guards
- [x] Free-text reject reason — `ApplicationRejection` aggregate + service-owned `application_rejections` side-table (unique per application; the seeded `applications` table has no writable column); `reject` accepts an optional reason (validated ≤1000 chars), surfaced on the applications listing (`ApplicationDto.RejectReason`) and kanban cards; unit tests
- [x] Recruiter pipeline board — Admin-portal kanban per requisition (role selector + stage columns, advance/reject on cards, live match %), now with **HTML5 drag-and-drop**: drag a non-terminal card onto a later column to chain the right number of `advance` calls, or onto Rejected to reject; only legal forward targets accept a drop (they highlight, backward/same-stage drops are ignored). Reuses the existing advance/reject endpoints (no backend change)
- [x] Applicant-facing application status timeline — "My applications" panel on the professional portal, live status per applied role (`GET /recruitment/talents/{id}/applications`)
- [x] Bulk actions — `POST /v1/recruitment/applications/bulk` advances/rejects many applications in one request (dedup, 200-item cap, per-item ok/status/error, single save, outbox event per success), writes admin-gated; unit tests; kanban card checkboxes + a bulk action bar (advance/reject/clear) in the admin portal

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
| Calendar integration (ICS/Google) | ✅ | Per-interview `.ics` invite + a subscribable per-talent calendar feed `GET /v1/recruitment/talents/{id}/calendar.ics` (multi-VEVENT, cancelled marked STATUS:CANCELLED) that Google/Outlook can subscribe to by URL |
| Interview scorecards / feedback | ✅ | Rating (1–5) + comment completes an interview |
| Panel interviews | ✅ | `interview_attendees` table + `GET/POST/DELETE /v1/recruitment/interviews/{id}/attendees`; the `.ics` invite lists the panel as ATTENDEE lines. Admin attendee-management UI is a follow-up |

- [x] Schedule interview (slot, location/mode, `.ics` invite) — schedule/list/cancel + `/interviews/{id}/ics`
- [x] Calendar sync — subscribable iCalendar feed `GET /v1/recruitment/talents/{id}/calendar.ics` (`Ics.BuildFeed` multi-VEVENT VCALENDAR with X-WR-CALNAME; cancelled interviews → STATUS:CANCELLED) that Google/Outlook subscribe to by URL and poll; repository joins interviews across the talent's applications; unit tests. A one-click "add to calendar" UI link is a follow-up
- [x] Panel interviews — `InterviewAttendee` aggregate + `interview_attendees` table; `GET/POST/DELETE /v1/recruitment/interviews/{id}/attendees` (writes admin-gated, name+email validation), and the `.ics` invite now emits an ATTENDEE line per panellist (mailto or invalid:nomail); domain/handler unit tests. Admin attendee-management UI is a follow-up
- [x] Interview scorecard + feedback capture — rating (1–5) + comment completes the interview

## F. Offers & onboarding
| Feature | Status | Notes |
|---|---|---|
| Offer management | ✅ | `offers` table in Recruitment; `/v1/recruitment/applications/{id}/offers` + `/offers/{id}/send\|accept\|decline\|withdraw` with a draft→sent→accepted/declined state machine (409 guards); admin pipeline offer drawer (draft & send, withdraw) |
| Offer letter / e-sign | ✅ | Rendered HTML offer letter (`GET /offers/{id}/letter`) with terms + signature block; candidate e-sign (`POST /offers/{id}/sign`, talent) records typed name + timestamp and accepts; admin "Letter" link |
| Onboarding checklist | ✅ | `onboarding_checklists` + `onboarding_tasks` in Recruitment; start-on-hire with default tasks, toggle/add/remove tasks + progress; `/v1/recruitment/applications/{id}/onboarding` + task endpoints; admin pipeline checklist in the offer drawer |

- [x] Offer create/accept/decline workflow — `Offer` aggregate + `offers` table, `/v1/recruitment` endpoints: create (admin), send/withdraw (admin), accept/decline (talent), list per application; draft→sent→accepted/declined/withdrawn state machine with 409 conflict guards; unit tests; admin pipeline "Offer" drawer (draft & send, status, withdraw). **Talent-side accept/decline/e-sign + letter view now live on the Professional & Student portals** (shared `TalentApplications` panel)
- [x] Onboarding checklist on hire — `OnboardingChecklist`/`OnboardingTask` aggregates + `onboarding_checklists`/`onboarding_tasks` tables (one checklist per application), start-with-default-tasks, toggle done, add/remove custom tasks, progress count; `/v1/recruitment/applications/{id}/onboarding` (get/start) + `/onboarding/tasks/{id}/toggle`, `/onboarding/{id}/tasks`, delete (writes admin-gated); unit tests; admin pipeline checklist in the offer drawer
- [x] Offer letter + e-sign — `Offer.Sign` (sent → accepted, capturing typed name + timestamp), `POST /offers/{id}/sign` (talent), `GET /offers/{id}/letter` rendering a formal HTML letter (terms + signature block) via a pure `OfferLetterHtml` renderer (HTML-escaped), `signed_by_name`/`signed_at` columns; unit tests; admin "Letter" link on offers

## G. Communication & notifications
| Feature | Status | Notes |
|---|---|---|
| Event-driven notifications (outbox) | ✅ | MassTransit outbox + Notifications worker |
| Transactional email (templated) | ✅ | Shared `Illumin360.Email` (MailKit/SMTP → Mailpit) + templates; Notifications worker emails on registration, application received, and application status change (recruitment events via the outbox) |
| In-app notification center | ✅ | Professional in-app notifications (list / mark-read / mark-all) fed by recruitment events (status change, job alerts); portal panel with unread count |
| In-app messaging (candidate↔employer) | ✅ | `application_messages` thread per application; `GET/POST /v1/recruitment/applications/{id}/messages` + `/messages/read`; recruiter message panel in the admin application drawer |
| Bulk email / campaigns | ✅ | `email_campaigns` + `campaign_recipients`; compose/add-recipients/send (`CampaignEmailRequested` outbox event per recipient → Notifications worker emails via SMTP); admin campaigns panel |

- [x] Email infrastructure — shared `Illumin360.Email` (MailKit SMTP → Mailpit) + templates; Notifications worker sends a **welcome email on registration** (verified end-to-end with a Testcontainers Mailpit)
- [x] Templated emails on application received / status change — Recruitment publishes `ApplicationSubmitted` / `ApplicationStatusChanged` via the outbox; Notifications worker consumers send the emails
- [x] In-app notification center — Professionals consume recruitment events (status change, job alerts) into a `professional_notifications` store; `/me/notifications` list + mark-read/mark-all + portal panel with unread count
- [x] Direct messaging between employer and candidate — `ApplicationMessage` aggregate + `application_messages` table (per-application thread, recruiter/talent sender, read receipts); `GET/POST /v1/recruitment/applications/{id}/messages` + `POST …/messages/read` (authenticated); unit tests; recruiter message thread in the admin application drawer. **Talent-side conversation UI now live on the Professional & Student portals** (shared `TalentApplications` panel — thread + composer + auto mark-read); push notification is a follow-up
- [x] Bulk email / campaigns — `EmailCampaign`/`CampaignRecipient` aggregates + `email_campaigns`/`campaign_recipients` tables; compose (draft) / add-remove recipients (idempotent, draft-only) / send; sending publishes a `CampaignEmailRequested` outbox event per recipient → new Notifications-worker `CampaignEmailConsumer` delivers over SMTP; unit tests; admin campaigns panel (compose, recipients, send, status)

## H. Employer / recruiter tooling
| Feature | Status | Notes |
|---|---|---|
| Employer self-registration (identity + role) | ✅ | via BFF `/register` |
| Employer/company profile service | ✅ | New `Illumin360.Employers` microservice — company profile get/register/update (`/v1/employers`), DB-per-service + migration + seed, gateway route |
| Employer portal UI | ✅ | `?portal=employer` page — company profile view + inline edit (industry/city/website/about) against `/api/employers/me`, plus a "top candidates for a role" panel wired to `/api/candidates/top` |
| Multi-user employer teams + roles | ✅ | `employer_team_members` table + `/v1/employers/me/team` list/invite/change-role/remove (owner/recruiter/viewer), "at least one owner" invariant, unique email per employer; team panel in the employer portal |
| Recruiter CRM (clients/contacts) | ✅ | `clients` + `client_contacts` tables in Recruitment; `/v1/recruitment/clients` list/create/status + contacts add/remove (prospect/active/inactive), seeded demo clients, Client CRM panel in the admin portal |
| Branded careers page | ✅ | Public server-rendered `/careers` (+ `/careers/{id}`) with SEO meta/Open Graph/canonical + schema.org ItemList & JobPosting JSON-LD, served via the gateway (no auth) |

- [x] Employers service — new `Illumin360.Employers` microservice (Domain/Application/Infrastructure/Api) with company profile get/register/update, DB-per-service (migrate + seed), gateway route `/api/employers/**`, unit + Testcontainers integration tests
- [x] Employers deploy — chiseled non-root Dockerfile + `employers-api` service in `docker-compose.apps.yml` (port 5206, gateway dependency; `illumin360_employers` DB already provisioned by the init script)
- [x] Employer portal UI — `?portal=employer` company-profile page: live profile view + inline edit (industry/city/website/about; company name fixed) via `PUT /api/employers/me`, and a "top candidates for a role" ranking panel (`GET /api/candidates/top`). Read-only snapshot fallback when the API is offline. Company **members/teams** are the follow-up
- [x] Employer team roles (owner/recruiter/viewer) — `TeamMember` aggregate + `employer_team_members` table (unique email per employer), `/v1/employers/me/team` list/invite/change-role/remove (writes admin-gated), "at least one owner" invariant guarding demotion & removal (409), seeded founding owner, unit + Testcontainers integration tests, and a team-management panel in the employer portal
- [x] Recruiter CRM (clients + contacts) — `Client`/`ClientContact` aggregates + `clients`/`client_contacts` tables (owned by Recruitment), `/v1/recruitment/clients` list (status filter) / create / change-status / add-contact / remove-contact (writes admin-gated), status lifecycle prospect→active→inactive, seeded demo clients, unit tests, and a Client CRM panel in the admin portal
- [x] Branded careers page / public SEO job pages — server-rendered `/careers` + `/careers/{id}` (Recruitment) with meta/Open Graph/canonical + schema.org `ItemList`/`JobPosting` JSON-LD, HTML-escaped, public via the gateway; `CareersHtml` renderer unit-tested

## I. Admin & governance
| Feature | Status | Notes |
|---|---|---|
| Verification queue (approve/reject) | ✅ | Admin service |
| Support tickets (assign/resolve) | ✅ | Backend + Admin portal panel live (assign + resolve) |
| User account management (suspend/activate) | ✅ | Backend + Admin portal panel live (suspend/activate) |
| Service-layer RBAC | ✅ | `Illumin360.Security` |
| Audit trail (viewable) | ✅ | Append-only `audit_log` in the Admin service — verification decisions, ticket triage and account status changes record entries; `GET /v1/admin/audit` (paged, action filter) + admin audit-trail table |
| GDPR data export / delete | ✅ | Subject-access **export** (`GET /v1/candidates/{id}/export`) + right-to-be-forgotten **erase** (`DELETE /v1/candidates/{id}` — removes candidate + notes + tags + pool memberships); admin "Export data" / "Erase" controls |

- [x] Admin portal panels for tickets + accounts (wire to existing APIs) — panels were already wired; completed by adding the ticket **Assign** action + assignee display (`Admin.tsx`)
- [x] Viewable audit trail — append-only `AuditEntry` + `audit_log` table in the Admin service; verification decide, ticket triage and account status handlers record entries (persisted in the same transaction); `GET /v1/admin/audit` (newest-first, action-prefix filter, paged) gated to admin readers; unit tests; audit-trail table in the admin portal
- [x] GDPR data export — `GetCandidateExportQuery` + `GET /v1/candidates/{id}/export` (admin-gated) returns a subject-access JSON of the candidate's profile, recruiter notes, tags and CV metadata (never the file bytes); unit tests; admin "Export data (GDPR)" link in the candidate drawer
- [x] GDPR erase-me — `EraseCandidateCommand` + `DELETE /v1/candidates/{id}` (admin-gated) permanently removes the candidate and all owned data (notes, tags, pool memberships) via EF `ExecuteDelete`; unit + Testcontainers integration test (round-trip: erase → 404 + empty notes/tags); admin "Erase (GDPR)" button with confirm. Cross-service erasure (other bounded contexts) is a follow-up

## J. Analytics & reporting
| Feature | Status | Notes |
|---|---|---|
| Recruitment stats / funnel / dashboards | ✅ | `/v1/recruitment/stats` + portal charts |
| Time-to-hire / source metrics | ✅ | `GET /v1/recruitment/metrics/hiring` — avg/median days apply→hire + source-of-hire (applications/hires per talent type); admin hiring-metrics panel |
| Custom reports / CSV-PDF export | ✅ | Source-of-hire + funnel reports as **CSV** (`ReportsCsv`) and **PDF** (dependency-free `ReportsPdf`) — `GET /v1/recruitment/reports/{name}.{csv\|pdf}` (admin-gated); admin download links |
| Diversity / EEO reporting | ✅ | `GET /v1/candidates/diversity` — anonymised aggregate counts by nationality / city / availability (no individual records); admin diversity-report panel |

- [x] Export reports to CSV + PDF — pure `ReportsCsv` (RFC-4180) and dependency-free `ReportsPdf` (minimal valid PDF 1.4 writer — catalog/page/Helvetica/content-stream, ASCII-sanitised + escaped) renderers; `GET /v1/recruitment/reports/source-of-hire.{csv|pdf}` and `/funnel.{csv|pdf}` (admin-gated) reusing the metrics/stats handlers; unit tests (CSV quoting + PDF envelope/length/escape); admin CSV & PDF download links on the hiring-metrics panel
- [x] Time-to-hire and source-of-hire metrics — `GetHiringMetricsQuery` + `GET /v1/recruitment/metrics/hiring` computes avg/median days from apply→hire-decision and applications/hires per source (talent type); pure `HiringMath` (avg/median) unit-tested; admin hiring-metrics panel (tiles + per-source conversion bars)
- [x] Diversity / EEO reporting — `GetDiversityReportQuery` + `GET /v1/candidates/diversity` (admin-gated) returns anonymised aggregate counts by nationality / city / availability (no individual records); handler unit test; admin diversity-report panel with share bars

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
- Total build items: 48
- Done: 50
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
- Recruiter CRM — `clients`/`client_contacts` in Recruitment + `/v1/recruitment/clients` CRUD, status lifecycle, seeded demo clients, admin-portal Client CRM panel (2026-08-10).
- Branded careers page — public SSR `/careers` + `/careers/{id}` with SEO meta + schema.org JSON-LD, gateway-exposed (2026-08-10).
- Offer management — `offers` in Recruitment + create/send/accept/decline/withdraw state machine, admin pipeline offer drawer (2026-08-10).
- Onboarding checklist — `onboarding_checklists`/`onboarding_tasks` in Recruitment, start-on-hire with default tasks + toggle/add/remove, admin pipeline checklist (2026-08-10).
- Talent-side offer + messaging UI — shared `TalentApplications` panel on the Professional & Student portals: expandable per-application view with offer accept/decline/e-sign + letter, and a two-way employer conversation (composer + auto mark-read) (2026-08-11).
- Kanban drag-and-drop — admin pipeline board cards are now draggable between stage columns (forward drop → chained advances, drop on Rejected → reject; legal targets highlight), reusing the existing advance/reject endpoints (2026-08-11).

_Update this file as items are ticked; link the commit/PR that delivered each._
