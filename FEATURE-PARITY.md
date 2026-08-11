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
| Talent pools / shortlists | ✅ | Named recruiter pools (`/v1/candidates/pools`) — create + add/remove candidates (dedup) + enriched members list; **admin-portal "Talent pools" panel** (create pool, expand to view/remove members, live counts) with "Add to shortlist" chips on each candidate-search result |

- [x] Matching engine (weighted city + role + skills) producing real scores — shared `Illumin360.Matching`, applied to **professional & student** matches (ranked by score) and the professional marketplace panel
- [x] "Recommended roles for you" — marketplace open roles ranked per professional (`POST /me/role-scores`, match % shown/sorted) — and the employer flip side, "top candidates for a role" (`GET /v1/candidates/top?title=&city=`)
- [x] Saved searches — talent CRUD + run-results (`/v1/recruitment/saved-searches`), professional-portal panel, plus a per-search **job-alerts opt-in** toggle
- [x] Scheduled alert-digest sender — `JobAlertScheduler` background service runs alert-enabled searches on an interval, publishes `JobAlertDigest` (outbox) → Notifications worker emails the matching roles
- [x] Shortlists / talent pools — named recruiter pools with create + add/remove candidates (dedup guard) + members listing (`/v1/candidates/pools`), admin-gated writes. **Admin-portal "Talent pools" panel**: create a pool, expand it to view/remove members with live counts, and "Add to shortlist" chips on each candidate-search result add that candidate to any pool (reuses the existing endpoints, no backend change)

## E. Interviews & scheduling
| Feature | Status | Notes |
|---|---|---|
| Interview scheduling | ✅ | Schedule/list/cancel interviews per application (`interviews` table + admin-gated endpoints); **admin pipeline "Interviews & panel" drawer** (schedule with datetime/duration/location, cancel, `.ics` link) |
| Calendar integration (ICS/Google) | ✅ | Per-interview `.ics` invite + a subscribable per-talent calendar feed `GET /v1/recruitment/talents/{id}/calendar.ics` (multi-VEVENT, cancelled marked STATUS:CANCELLED) that Google/Outlook can subscribe to by URL |
| Interview scorecards / feedback | ✅ | Rating (1–5) + comment completes an interview |
| Panel interviews | ✅ | `interview_attendees` table + `GET/POST/DELETE /v1/recruitment/interviews/{id}/attendees`; the `.ics` invite lists the panel as ATTENDEE lines. **Admin attendee-management UI** now live in the pipeline "Interviews & panel" drawer (expand an interview → add/remove panellists with name/email/role) |

- [x] Schedule interview (slot, location/mode, `.ics` invite) — schedule/list/cancel + `/interviews/{id}/ics`
- [x] Calendar sync — subscribable iCalendar feed `GET /v1/recruitment/talents/{id}/calendar.ics` (`Ics.BuildFeed` multi-VEVENT VCALENDAR with X-WR-CALNAME; cancelled interviews → STATUS:CANCELLED) that Google/Outlook subscribe to by URL and poll; repository joins interviews across the talent's applications; unit tests. A one-click "add to calendar" UI link is a follow-up
- [x] Panel interviews — `InterviewAttendee` aggregate + `interview_attendees` table; `GET/POST/DELETE /v1/recruitment/interviews/{id}/attendees` (writes admin-gated, name+email validation), and the `.ics` invite now emits an ATTENDEE line per panellist (mailto or invalid:nomail); domain/handler unit tests. **Admin attendee-management UI**: the pipeline drawer now hosts an "Interviews & panel" section (schedule/list/cancel interviews, per-interview `.ics` link, and expand an interview to add/remove panellists with name/email/role) over the existing endpoints — no backend change
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
| In-app notification center | ✅ | Professional in-app notifications (list / mark-read / mark-all) fed by recruitment events (status change, job alerts); portal panel with unread count. **Browser push**: a service worker + Notifications API raise an OS toast for each newly-arrived unread item (opt-in "Enable push"; the SW carries a `push` handler ready for a future server-side Web Push/VAPID upgrade) |
| In-app messaging (candidate↔employer) | ✅ | `application_messages` thread per application; `GET/POST /v1/recruitment/applications/{id}/messages` + `/messages/read`; recruiter message panel in the admin application drawer |
| Bulk email / campaigns | ✅ | `email_campaigns` + `campaign_recipients`; compose/add-recipients/send (`CampaignEmailRequested` outbox event per recipient → Notifications worker emails via SMTP); admin campaigns panel |

- [x] Email infrastructure — shared `Illumin360.Email` (MailKit SMTP → Mailpit) + templates; Notifications worker sends a **welcome email on registration** (verified end-to-end with a Testcontainers Mailpit)
- [x] Templated emails on application received / status change — Recruitment publishes `ApplicationSubmitted` / `ApplicationStatusChanged` via the outbox; Notifications worker consumers send the emails
- [x] In-app notification center — Professionals consume recruitment events (status change, job alerts) into a `professional_notifications` store; `/me/notifications` list + mark-read/mark-all + portal panel with unread count
- [x] Direct messaging between employer and candidate — `ApplicationMessage` aggregate + `application_messages` table (per-application thread, recruiter/talent sender, read receipts); `GET/POST /v1/recruitment/applications/{id}/messages` + `POST …/messages/read` (authenticated); unit tests; recruiter message thread in the admin application drawer. **Talent-side conversation UI now live on the Professional & Student portals** (shared `TalentApplications` panel — thread + composer + auto mark-read); **browser push** now surfaces new notifications as OS toasts (service worker + Notifications API, opt-in)
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
- Feature-matrix rows: 64 — **63 ✅ have, 1 ⬜ missing** (mobile app, out of scope)
- Build checklist: **48 / 48 delivered**; deep-audit backlog **fully engineering-complete — Tier 1 9/9 (+ both #1 sub-items) and Tier 2 7/7 (incl. semantic-matching v1 and the full LTR capture→train→evaluate→serve loop)**. 0 in progress, 0 unchecked.
- Residual (non-engineering — production data or a governance/scope decision): enable a real embedding model (data-egress sign-off); native mobile app (out of scope). Live learned ranking is now wired end-to-end (flag-gated, self-falls-back until the model beats the heuristic on real data).
- **Final re-sweep (2026-08-11):** 9 unit-test suites green — **316 tests, 0 failures** (Recruitment 149, Candidates 54, Matching 31, Professionals 23, Students 18, Employers 16, Admin 11, Resume 10, Email 4); business-portal build clean; apply-time talent-features + all outcome/model endpoints + the flag-gated live-ranking endpoint confirmed; all 9 arc EF migrations in Recruitment (through `TalentSideFeatures`) + Candidates `CandidateCustomFields` present; `main` clean. LTR now trains on a 9-feature vector (score + pipeline + talent-side signals) and re-ranks live applicants when enabled + it beats the heuristic

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
- Talent-pool recruiter UI — admin-portal "Talent pools" panel (create pool, expand to view/remove members, live counts) + "Add to shortlist" chips on candidate-search results, over the existing `/v1/candidates/pools` endpoints (2026-08-11).
- Panel-interview attendee UI — admin pipeline "Interviews & panel" drawer: schedule/list/cancel interviews, per-interview `.ics` link, and add/remove panellists (name/email/role), over the existing interview + attendee endpoints (2026-08-11).
- Browser push notifications — service worker (`public/sw.js`) + Notifications API (`push.ts`) raise an OS toast for each newly-arrived unread in-app notification on the Professional portal (opt-in; polls the existing `/me/notifications` feed; SW `push` handler ready for a future server-side Web Push/VAPID sender) (2026-08-11).
- Configurable application forms / screening questions (Tier 1 #1) — Recruitment `application_form_questions` + `application_answers` tables/endpoints (EF migration `ApplicationForms`), admin pipeline form-builder + candidate answers in the application drawer; 4 new unit tests (2026-08-11).
- Candidate apply-time form capture (Tier 1 #1 sub-task) — `ApplyForm` modal on the Professional marketplace apply: roles with screening questions collect answers (required-validated) then apply + POST answers; roles without questions keep one-click apply (2026-08-11).
- Employee referrals + internal-only job toggle (Tier 1 #2) — Recruitment `referrals` table + submit/list endpoints, `internal` flag on `RequisitionDetail` (`PUT …/internal`) that hides a role from public careers; migration `ReferralsAndInternalRoles`; admin referral panel + internal toggle; 4 unit tests (2026-08-11).
- Source / channel attribution (Tier 1 #3) — Recruitment `application_sources` table (channel per application), apply records source, `GET/PUT /applications/{id}/source` + `GET /metrics/channels`; migration `ApplicationSources`; admin drawer source selector + channel breakdown panel; Professional applies tag "careers"; 5 unit tests (2026-08-11).
- Bulk CSV candidate import (Tier 1 #4) — pure `CandidateCsv` RFC-4180 parser + `POST /v1/candidates/import` (register + name/city dedupe + per-row errors); admin "Bulk import candidates" panel (paste/upload + summary); 5 unit tests, no migration (2026-08-11).
- Job distribution / syndication (Tier 1 #5) — `CareersSyndication` renderer + public `/careers/feed.xml` (RSS), `/careers/sitemap.xml`, `/careers/feed.json` (internal roles excluded, absolute URLs) + social-share links on role detail pages; 3 unit tests, no migration (2026-08-11).
- Structured multi-round interviews (Tier 1 #6) — interview round label + required skills, per-round skill ratings (`interview_skill_ratings`) via `GET/POST /interviews/{id}/skill-ratings`, and `GET /applications/{id}/interview-summary` aggregating skill averages across rounds; migration `MultiRoundInterviews`; admin drawer round/skills scheduling + 1–5 scoring + averages panel; 4 unit tests (2026-08-11).
- Email-to-ATS intake (Tier 1 #7) — `POST /v1/candidates/intake/email` parses an emailed résumé into a candidate stub (name from sender/filename, skills headline via Illumin360.Resume, name+city dedupe, CV attached to MinIO when supported); pure `EmailIntake` helpers; 4 unit tests, no migration (2026-08-11).
- Richer careers search + per-job analytics (Tier 1 #8) — careers index `?q=`/`?remote=` filtering with an on-page form + Remote badges; `career_views` counter incremented per detail view + `GET /metrics/careers-views` + admin "Careers page views" panel; migration `CareerViews`; 3 unit tests (2026-08-11).
- Featured/paid job listings (Tier 1 #9) — `featured_until` promotion on `RequisitionDetail` via `PUT /requests/{id}/feature`; featured roles float to the top of the public careers site with a badge; admin Feature 7d/30d/Unfeature control; migration `FeaturedListings`; 3 unit tests. Closes the last core Tier 1 gap (2026-08-11).
- Candidate custom fields (Tier 1 #1 sub-task) — Candidates `custom_field_definitions` + `candidate_custom_values`; define/list/remove fields + get/set per-candidate values; migration `CandidateCustomFields`; admin definition panel + per-candidate editor; 4 unit tests. **All Tier 1 items now delivered** (2026-08-11).
- Skill-gap analysis (Tier 2) — pure `SkillGapAnalyzer` in `Illumin360.Matching` + Professionals `POST /me/skill-gap`; professional-portal "Skill gap for a role" tool (coverage % + matched/missing chips); 4 unit tests, no migration (2026-08-11).
- Salary/seniority scoring (Tier 2) — `MatchScorer` blends optional salary-vs-band + seniority-ladder signals (renormalised so base scores are unchanged when absent) + a `SeniorityParser`; seniority auto-wired into professional role-scores; 9 new unit tests, no migration (2026-08-11).
- Explainable matches (Tier 2) — `MatchScorer.Explain` returns per-signal contributions + reasons; Professionals `POST /me/role-explanation` + a "Why?" expander on marketplace roles; 3 new unit tests, no migration (2026-08-11).
- Similar candidates (Tier 2) — pure `CandidateSimilarity` ranker + Candidates `GET /{id}/similar`; admin "Similar candidates" list in the search expander; 2 new unit tests, no migration (2026-08-11).
- Blind screening (Tier 2) — `BlindRedactor` + `?blind=true` on candidate search anonymises name/nationality server-side; admin "Blind screening" toggle; 2 new unit tests, no migration (2026-08-11).
- Semantic matching v1 (Tier 2) — hashing `IEmbeddingProvider` + `VectorMath` + `SemanticRanker` (compute-on-query, no egress); Candidates `GET /{id}/semantic-similar` behind `Matching:SemanticEnabled` (off by default); admin self-hiding "Semantically similar" panel; 5 new unit tests, no migration (2026-08-11).
- Hire-outcome capture (Tier 2 LTR groundwork) — `match_outcomes` table records a labelled row on every terminal decision (hire/reject) with the ranker's score; `GET /metrics/outcomes` + admin "Hiring outcomes" panel; migration `MatchOutcomes`; 4 new unit tests. Builds the training set so learning-to-rank becomes viable once enough labels accrue (2026-08-11).
- Outcome feature enrichment (Tier 2 LTR) — `match_outcomes` gains a Recruitment-owned feature snapshot (source, remote, interview count, avg interview rating, offer-made, days-to-decision) captured at decision time + `GET /metrics/outcomes/export.csv` (features-first/label-last) and an admin CSV export; migration `MatchOutcomeFeatures`; +3 unit tests (Recruitment 142/142) (2026-08-11).
- LTR train/eval/serve loop (Tier 2) — dependency-free logistic-regression ranker + deterministic hold-out evaluator (AUC/accuracy/log-loss vs heuristic) + servable `RankModel` in `Illumin360.Matching`; Recruitment `GET /metrics/outcomes/model` (train-on-demand, gated ≥ 20 decisions/both classes) + admin "Learned ranking model" panel; 6 new unit tests (Matching 31, Recruitment 145). Closes the feedback-loop item's engineering (2026-08-11).
- Apply-time talent-side features (Tier 2 LTR) — `application_features` side-table captures the Professional portal's city/role/skill signal points at apply-time; folded into `MatchOutcome` (+3 columns) + the model's 9-feature vector + CSV export; migration `TalentSideFeatures`; +2 unit tests (Recruitment 146) (2026-08-11).
- Live learned ranking (Tier 2 LTR — serve into production) — Recruitment `GET /requests/{id}/applications/ranked` (behind `Matching:LearnedRankingEnabled`, off by default) trains + evaluates on the captured outcomes and re-ranks the requisition's live applicants by predicted hire likelihood **only when the learned model beats the heuristic on the deterministic hold-out**, else transparently falls back to match-score order; shared `OutcomeFeatures.VectorOf` builds the identical 9-feature vector at train + score time from the in-pipeline snapshot; admin "Live learned ranking" panel surfaces the order + why. No migration (scores existing data); +3 unit tests (Recruitment 149) (2026-08-11).
- Live-ranking flag enabled + end-to-end verified — `Matching:LearnedRankingEnabled` turned on in Recruitment `appsettings.Development.json`; new `Illumin360.Recruitment.IntegrationTests` project drives the real HTTP pipeline (routing → JWT bearer + AdminPolicy → handler → real `Illumin360.Matching` model → JSON) on a Testcontainers PostgreSQL: 401-without-token, flag-off heuristic fallback, and flag-on learned reorder (3 tests, all green) (2026-08-11).
- Seeded docker demo profile — `deploy/docker/docker-compose.demo.yml` overlay flips the flag on for `recruitment-api` and adds a one-shot `demo-seed` service that (after the startup migration) applies an idempotent SQL seed (`deploy/docker/demo/seed-recruitment-demo.sql`): one requisition, six in-pipeline applicants whose match-score order deliberately differs from their learned order, and 24 labelled outcomes (match score overlapping, signal in interviews/rating/offer) so the model beats the heuristic. A 4th integration test runs the exact deploy SQL against the **real repository** + Testcontainers PostgreSQL and asserts the endpoint returns `usedModel=true` and re-orders (the highest-match applicant is not ranked first) (2026-08-11).

_Update this file as items are ticked; link the commit/PR that delivered each._

---

## Deep parity audit (2026-08-11)

A second, deeper pass that researched what the 10 reference systems **actually ship** (modules, features,
actions) and mapped their distinctive capabilities against Illumin360 — rather than re-checking our own
claims. Below is the residual backlog. Tick `[x]` as delivered, same convention as the main checklist.

**Research caveats:** *OpenJobs* has no canonical repo (the closest match is a ~5-commit template — low signal);
*TalentMatch* is not a single project (benchmarked against the embedding/LTR technique landscape); *OrangeHRM*'s
richest ATS features are paid-edition, so its free OSS core is thinner than its marketing implies.

### Tier 1 — genuine parity gaps (peers commonly have; Illumin360 does not)
- [x] **Configurable application forms / screening questions per job** — Recruitment service owns `application_form_questions` (per requisition: label, kind text/textarea/boolean/number/select, options, required, order) + `application_answers` (per application, label snapshotted); `GET/POST /requests/{id}/form`, `DELETE /form/questions/{id}` (admin-write), `GET/POST /applications/{id}/answers` (auth). Admin pipeline **form-builder** (add/remove questions per role) + candidate **answers shown in the application drawer**; domain + handler unit tests (OpenCATS, SpotAxis, OrangeHRM, Horilla, Frappe). *Remaining sub-task ↓ candidate apply-time capture.*
  - [x] Candidate apply-time form capture — `ApplyForm` modal on the Professional marketplace apply: roles with questions open the form (text/textarea/boolean/number/select, required-validated), then apply → POST answers to `/applications/{id}/answers`; roles without questions keep one-click apply. (Student has no live recruitment-request apply flow, so nothing to wire there yet)
  - [x] Admin-defined **custom fields** on candidate records — Candidates service `custom_field_definitions` + `candidate_custom_values` tables; `GET/POST/DELETE /v1/candidates/custom-fields` (label→auto key, kind text/number/boolean/select, unique-key guard) and `GET/PUT /v1/candidates/{id}/custom-values` (get returns every field with blanks for unset; set replaces, skipping blanks/unknown); EF migration `CandidateCustomFields`; admin "Candidate custom fields" definition panel + per-candidate values editor in the search expander; 4 unit tests (OpenCATS, SpotAxis). *(Company-record custom fields — a small follow-up in the Employers service if wanted.)*
- [x] **Employee referrals + internal-only job toggle** — Recruitment `referrals` side-table (per requisition: referrer, candidate name/email, note) + `GET/POST /requests/{id}/referrals` (submit any signed-in user, list admin), and an `internal` flag on `RequisitionDetail` via `PUT /requests/{id}/internal` that **hides the role from the public careers site** (index filter + detail 404); EF migration `ReferralsAndInternalRoles`; admin pipeline referral panel + "Internal only" toggle; 4 unit tests (EazyRecruit, Frappe, Horilla)
- [x] **Candidate source / channel attribution** — Recruitment `application_sources` side-table (1:1 per application, normalised channel — direct/careers/referral/campaign/board/agency/walk-in); apply records a channel (`ApplyToRequestBody.source`, defaults "direct"), `GET/PUT /applications/{id}/source` view/override, and `GET /metrics/channels` gives applications+hires by channel; EF migration `ApplicationSources`; Professional marketplace applies tag "careers"; admin drawer source selector + "Source of applications" breakdown panel; 5 unit tests (OpenCATS, OrangeHRM, Frappe)
- [x] **Bulk CSV import** of candidates — Candidates service `POST /v1/candidates/import` (admin-write) parses CSV via a pure RFC-4180 `CandidateCsv` parser (header maps firstName/lastName/city/nationality[/availability/headline] by name; quoted fields with commas/newlines), registers each new candidate, **dedupes by name+city** (against existing + within the batch), and returns `{created, skipped, errors[]}` with per-row problems; admin "Bulk import candidates" panel (paste or upload CSV + result summary); 5 unit tests (OpenCATS, Jobberbase). *(Job-order CSV import — separate follow-up if needed.)*
- [x] **Job distribution / syndication** — pure `CareersSyndication` renderer + public endpoints off the existing SSR careers site: **RSS 2.0** (`/careers/feed.xml`, autodiscovery `<link>` on the index), **`sitemap.xml`** (`/careers/sitemap.xml`), and a **JSON feed** (`/careers/feed.json`) for aggregation/embedding — all with absolute URLs from the request origin and **internal-only roles excluded**; plus **social-share** links (X/LinkedIn/Facebook/email, client-side from the page URL) on each role detail page; 3 unit tests (SpotAxis, Horilla, GitJobs, Jobberbase, OpenCATS). *(Direct LinkedIn/board API multiposting — a further follow-up needing per-board API credentials.)*
- [x] **Structured multi-round interviews** — interviews now carry a **round** label + **required skills**; per-round **skill ratings** (1–5) via `GET/POST /interviews/{id}/skill-ratings` (`interview_skill_ratings` table); and `GET /applications/{id}/interview-summary` **aggregates skill averages across all rounds** + lists the rounds. EF migration `MultiRoundInterviews`; admin pipeline drawer: schedule with round + skills-to-assess, per-round 1–5 skill scoring, and a "Skill averages across rounds" panel; 4 unit tests (Frappe, OrangeHRM, EazyRecruit). *(Reusable interview question banks/kits — a further follow-up.)*
- [x] **Email-to-ATS intake** — `POST /v1/candidates/intake/email` ingests an emailed résumé (from-name/email, subject, base64 attachment) into a candidate stub: derives the name from the sender/filename, parses the attachment via `Illumin360.Resume` to build a skills headline, dedupes by name+city, and **attaches the CV** to the candidate (MinIO) when it's a supported type; pure `EmailIntake` helpers + handler; 4 unit tests (EazyRecruit). The endpoint is the ingestion contract a mailbox/IMAP poller calls with an admin-write service identity — *(the poller worker itself is a deployment-side follow-up).*
- [x] **Richer public-careers search + per-job analytics** — the SSR careers index now takes `?q=` (title/city keyword) + `?remote=true` filters with an on-page filter form and **Remote** badges on cards; and a `career_views` counter increments on each detail-page view, exposed via `GET /v1/recruitment/metrics/careers-views` (admin) with a "Careers page views" admin panel; EF migration `CareerViews`; 3 unit tests (GitJobs). *(Salary/seniority/category/skill facets on the public page — a further follow-up; seniority isn't modelled yet.)*
- [x] **Featured / paid job listings** — a `featured_until` promotion window on `RequisitionDetail` via `PUT /requests/{id}/feature` (`{days}`; ≤ 0 clears): featured roles **float to the top of the public careers site** with a gold "Featured" badge/highlight; admin role-details "Feature 7d / 30d / Unfeature" control showing the active window; EF migration `FeaturedListings`; 3 unit tests (Jobberbase, GitJobs). *(Payment capture itself is handled out-of-band — the promote action is the ATS hook a payment gateway would gate; no in-app payment processing.)*

### Tier 2 — matching depth (differentiators; largely absent in the peers too)
Our engine today is a weighted heuristic (city + role + skills). The modern-matcher benchmark adds:
- [x] **Semantic / embedding matching (v1)** — `IEmbeddingProvider` + deterministic `HashingEmbeddingProvider` (FNV-1a feature-hashing, unit-normalised — no external calls, no data egress), `VectorMath` (cosine/normalise), and `SemanticRanker` (compute-on-query cosine k-NN) in `Illumin360.Matching`; Candidates `GET /{id}/semantic-similar` **behind `Matching:SemanticEnabled` (off by default)** — returns empty when off so the admin "Semantically similar" panel self-hides; candidate-side only for v1. 5 new unit tests. **Design/decisions:** [`03-architecture/semantic-matching-design.md`](03-architecture/semantic-matching-design.md). *Real semantics (hosted/self-hosted model + pgvector persistence) is a config/infra swap behind the same interface — deferred pending a data-egress decision; feedback-loop LTR remains separate.*
- [x] **Similar candidates ("more like this")** — pure `CandidateSimilarity` ranker in `Illumin360.Matching` (blends city 0.40 / availability 0.20 / headline-token Jaccard 0.40, seed excluded, zero-similarity dropped); Candidates `GET /v1/candidates/{id}/similar?take=` returns the closest matches with scores; admin "Similar candidates" list in the candidate-search expander (click to pivot). 2 new unit tests. Dependency-free k-NN over candidate attributes (candidates carry no structured skills — those live on Professionals)
- [x] **Skill-gap analysis** — pure `SkillGapAnalyzer` in `Illumin360.Matching` (matched / missing / extra + coverage %, case-insensitive, required-order preserved); Professionals `POST /me/skill-gap` compares the profile's skills to a role's required skills; professional-portal "Skill gap for a role" tool (coverage bar + matched ✓ chips + "to learn" chips); 4 unit tests (drives upskilling suggestions)
- [x] **Salary-expectation & seniority scoring** — `MatchScorer` now blends two optional signals into the composite: **salary** (candidate expectation vs the role's band — full score within/below the band, decaying above the ceiling) and **seniority** (via a `SeniorityParser` ordinal ladder — exact level 1.0, one band off 0.5, else 0.0). Weights renormalise so callers passing only city/role/skills score exactly as before. **Seniority is auto-wired** into the professional marketplace role-scores (derived from headline vs title text — no new fields); salary is engine-ready and consumed wherever a caller supplies a numeric expectation. 9 new unit tests. *(A stored salary-expectation profile field to feed salary scoring end-to-end is a thin follow-up.)*
- [x] **Explainable "why this match"** — `MatchScorer.Explain` returns a `MatchExplanation` (score + per-signal `MatchSignal`: normalised weight, raw 0–1, point contribution, human reason) for City/Role/Skills/Salary/Seniority; `Score` delegates to it so there's one source of truth. Professionals `POST /me/role-explanation` + a **"Why?"** expander on each marketplace role showing each signal's points + reason. 3 new unit tests. *No OSS peer does this — a real differentiator.*
- [x] **Feedback-loop learning (train/eval/serve loop)** — the full loop, dependency-free: labelled `match_outcomes` capture (score + feature snapshot: source, remote, interview count, avg interview rating, offer-made, days-to-decision) → **train** a pointwise LTR model (`LogisticRegressionTrainer`, deterministic gradient descent + feature standardisation in `Illumin360.Matching`) → **evaluate** on a deterministic hold-out (`RankEvaluator`: AUC/accuracy/log-loss, learned model vs the current match-score heuristic) → **serve** a `RankModel` that scores a feature vector 0–100. On-demand `GET /metrics/outcomes/model` (gated on ≥ 20 decisions with both classes) returns the metrics + learned weights; admin "Learned ranking model" panel with a Train & evaluate button; `GET /metrics/outcomes/export.csv` for offline training. **Talent-side features now captured at apply-time** — the Professional portal sends its city/role/skill signal points with the application (`application_features` side-table), folded into each outcome + the model's 9-feature vector (Recruitment can't compute these cross-service). 8 new unit tests. **Now promoted into live scoring** — Recruitment `GET /requests/{id}/applications/ranked` (behind `Matching:LearnedRankingEnabled`, off by default) trains + evaluates on the captured outcomes and, only when the model beats the heuristic on the hold-out, re-ranks the requisition's live applicants by predicted hire likelihood (same 9-feature vector, in-pipeline snapshot); otherwise it transparently falls back to match-score order. Admin "Live learned ranking" panel surfaces the order + why. 3 more unit tests.
- [x] **Blind screening** — data-minimised candidate search: `GET /v1/candidates/search?blind=true` runs the redaction server-side via a pure `BlindRedactor` (name → a stable anonymous handle like "Candidate 7F3A", nationality → "—"; city/availability/headline + the id kept so reviewers assess on merit and can still act); admin candidate-search **"Blind screening"** toggle; 2 new unit tests. *(Fairness / adverse-impact auditing over hiring outcomes remains a larger follow-up.)*

### Tier 3 — out of scope (correctly excluded, no action)
HRMS breadth carried by OrangeHRM / Frappe HR / Horilla that does **not** belong in a talent marketplace:
payroll, attendance/time tracking, leave management, performance/appraisal, employee lifecycle &
convert-applicant-to-employee/HRIS, org chart, asset management, expense claims, shift scheduling, LMS;
plus SpotAxis's multi-tenant SaaS billing and Frappe's staffing-plan/headcount planning. Same category as
the native **mobile app** — a separate product decision, not a parity gap.

---

## v0.3.0 — Commercial parity audit (2026-08-11)

The first two audits benchmarked against **open-source** peers (all of Tier 1 + Tier 2 now shipped in
v0.2.0). This third pass benchmarks against **10 commercial systems** to plan v0.3.0 — where the market has
moved since the OSS tools were built.

**Reference systems (commercial):** Greenhouse, Lever, Workday Recruiting, iCIMS, SmartRecruiters, Ashby,
Bullhorn, LinkedIn Talent Solutions, Upwork, Eightfold AI. *(ATS/CRM · enterprise talent cloud · modern
all-in-one · staffing-agency · professional graph · freelance marketplace · talent-intelligence.)*

**Where we already hold parity or lead** (built in v0.1–0.2): structured pipeline + kanban + bulk actions,
configurable application forms & screening questions, source attribution, structured multi-round interviews
+ scorecards, offers + e-sign + onboarding checklist, talent pools, saved searches + job alerts, careers
SSR/SEO + syndication feeds + featured listings, recruiter CRM, employer teams, in-app messaging + bulk
email, audit trail + GDPR export/erase, diversity aggregates + time-to-hire/source metrics, and — ahead of
every OSS peer — **explainable matching, semantic-matching v1, and a full learning-to-rank loop now serving
live ranking**. The commercial gap is concentrated in four themes below.

**Legend:** ✅ have · 🟡 partial · ⬜ missing. `[DECISION]` = needs a product/governance call before build.

### Capability matrix vs. commercial peers
| Capability | Us | Exemplars |
|---|---|---|
| **AI / GenAI** | | |
| Learned ranking (LTR) serving live | ✅ | SmartAssistant, Lever Talent Fit |
| Real embedding matching + vector store (pgvector) | 🟡 v1 hashing, flag-gated | Ashby, Eightfold |
| GenAI assistant (JD-gen, candidate summaries, message drafting) | ⬜ | all 8 ATS |
| Conversational apply / screening chatbot (multi-channel) | ⬜ | iCIMS Digital Assistant, Winston |
| Autonomous AI agents (source/screen/schedule) | ⬜ | LinkedIn Hiring Assistant, Illuminate, Amplify |
| MCP / LLM tool server over our data | ⬜ | Greenhouse, Ashby |
| **Skills intelligence** | | |
| Structured skills + proficiency + endorsements | ✅ | LinkedIn |
| Skills taxonomy / ontology (normalized, synonyms) | ⬜ | Workday Skills Cloud, Eightfold |
| Skills inference from work history | ⬜ | Eightfold |
| Career pathing / development recommendations | ⬜ | Eightfold, Workday Career Hub |
| **Engagement / CRM** | | |
| Nurture sequences / drip campaigns (multi-step, triggered) | 🟡 one-shot bulk email | Lever Nurture, Ashby sequences |
| SMS / text recruiting | ⬜ | iCIMS, SmartRecruiters, Bullhorn |
| Omnichannel messaging (email+SMS+WhatsApp threaded) | 🟡 email+in-app+push | SmartRecruiters SmartMessage |
| Talent rediscovery / silver-medalist re-engagement | ⬜ | Workday, iCIMS, Eightfold |
| Sourcing browser extension (capture to CRM) | ⬜ | Greenhouse, Ashby |
| **Interviewing** | | |
| Structured scorecards + multi-round skill ratings | ✅ | Greenhouse |
| Self-schedule booking links + availability engine | ⬜ | Ashby, Greenhouse, iCIMS |
| Multi-interviewer / timezone / load-balancing scheduling | 🟡 panels, no availability | Ashby |
| Reusable interview kits / question banks | ⬜ | Greenhouse, Lever |
| Interview intelligence (recording / transcript / AI notes) | ⬜ | Greenhouse Notetaker, Lever |
| **Analytics** | | |
| Funnel / time-to-hire / source-of-hire | ✅ | all |
| Custom / interactive report builder | ⬜ | Ashby, Lever Data Explorer |
| Capacity / hiring forecasting | ⬜ | Ashby Recruiting Planner |
| DEI representation at each funnel stage | 🟡 aggregate only | Ashby |
| Quality-of-hire surveys | ⬜ | Ashby |
| Data-warehouse sync / BI export | 🟡 CSV/PDF | Lever, Ashby |
| **Distribution** | | |
| SSR careers + SEO + featured + syndication feeds | ✅ | Greenhouse |
| Programmatic job advertising / board API multiposting | 🟡 RSS/sitemap/JSON | SmartRecruiters SmartJobs, Workday |
| Apply Connect / LinkedIn Job Wrapping | ⬜ | LinkedIn |
| Careers personalization + landing pages + ADA | 🟡 SSR+search+featured | iCIMS Attract |
| **Hiring ops / compliance** | | |
| Audit trail + GDPR export/erase | ✅ | all |
| Multi-step offer / req approval routing | 🟡 req approval + offer send | Workday, iCIMS, Ashby |
| Real e-signature provider (DocuSign-class) | 🟡 typed-name e-sign | Greenhouse, Ashby |
| Background-check integration | ⬜ | Checkr partners |
| Onboarding docs / e-forms / I-9 | 🟡 task checklist | iCIMS, Bullhorn |
| OFCCP/EEOC self-ID + federal reporting | 🟡 diversity aggregates | Greenhouse, Lever |
| Assessments integration (coding/skills tests) | ⬜ | Ashby, Greenhouse |
| **Platform / ecosystem** | | |
| REST API + OpenAPI + internal outbox | ✅ | all |
| Public API keys + outbound webhook subscriptions | 🟡 internal only | all |
| App marketplace / partner catalog | ⬜ | Greenhouse 500+, SmartRecruiters 350+ |
| Enterprise SSO (SAML) + SCIM provisioning | 🟡 OIDC (Keycloak) | enterprise ATS |
| **Marketplace-native (Upwork/Eightfold axis)** | | |
| Payments / escrow / milestones / contracts | ⬜ (deliberate) | Upwork |
| Two-sided reviews / reputation score | ⬜ | Upwork JSS |
| Worker classification / compliance / EOR | ⬜ | Upwork Enterprise |
| Contingent-workforce visibility / VMS / pay-bill | ⬜ | Bullhorn, Workday VNDLY, Eightfold Flex |
| Skill badges / assessments / vetting | 🟡 endorsements | Upwork badges |

### v0.3.0 committed scope (decisions locked 2026-08-12)
Three directional forks were decided by the product owner:
1. **AI / data egress → hosted, opt-in per tenant.** Hosted LLM/embedding APIs are allowed behind a
   per-tenant flag (default **off**), governed by a data-processing addendum. Unlocks the real-embedding
   upgrade + GenAI assistant this milestone (chatbot/agents remain Tier 2). Needs a DPA + egress sign-off
   before the flag ships on.
2. **Marketplace → full transaction layer.** Illumin360 becomes *transactional*: payments/escrow,
   milestones, contracts, and two-sided reviews/reputation are **in scope for v0.3.0** (moved up from Tier 3).
   This is the milestone's largest workstream — **design-doc-first**, and it needs a `[DECISION: payment
   provider]` (e.g. Stripe Connect / escrow partner) + a worker-classification/compliance stance.
3. **Tier 1 headliners → all four committed:** talent rediscovery, nurture sequences, self-schedule +
   interview kits, skills taxonomy v1.

**v0.3.0 = three workstreams:** (A) AI-native — embeddings+pgvector, GenAI assistant [hosted opt-in];
(B) Recruiting depth — rediscovery, nurture sequences, self-schedule + interview kits, skills taxonomy v1;
(C) Transactional marketplace — payments/escrow, milestones, contracts, reviews/reputation [design-doc first].
Tracked as GitHub milestone **v0.3.0** with one issue per item.

### Proposed v0.3.0 backlog (tiered)

**Tier 1 — headliners (high fit; build largely on what we have).** Ordered by fit×leverage:
- [ ] **Talent rediscovery / silver-medalists** — re-evaluate past applicants (incl. rejected) against a new requisition using the existing matching engine + `match_outcomes` + talent pools. *Pure engineering, no external dep — highest-fit item.*
- [ ] **Nurture sequences / drip campaigns** — extend one-shot `email_campaigns` into multi-step, triggered sequences (delays, stage/segment triggers, stop-on-reply). *Engineering.*
- [ ] **Self-schedule interview booking** — candidate self-booking links over interviewer availability windows (timezone-aware), reusing interviews/attendees. *Engineering.*
- [ ] **Reusable interview kits / question banks** — per-role kits mapping questions → the skills already scored per round. *Engineering (flagged follow-up in v0.2).*
- [ ] **Skills taxonomy / ontology v1** — normalize + dedup skills, synonym mapping, canonical skill ids feeding matching. *Can ship dependency-free first (like hashing-embeddings v1), inference later.*
- [ ] **Real embedding model + pgvector** `[DECISION: data egress]` — swap the flag-gated hashing provider for a hosted/self-hosted model + pgvector persistence, behind the existing `IEmbeddingProvider`. *Already designed (`03-architecture/semantic-matching-design.md`); gated on the egress call.*
- [ ] **GenAI assistant** `[DECISION: LLM egress]` — JD generation, candidate/CV summarization, message drafting behind a provider flag (self-hostable). *Gated with the embedding decision.*
- [ ] **SMS / text recruiting** `[DECISION: SMS provider]` — a second notification channel (status, interview reminders, nurture) via a pluggable provider (e.g. Twilio), mirroring the `Illumin360.Email` building block.

**Tier 2 — differentiators / larger or externally-gated:**
- [ ] Custom/interactive analytics report builder + hiring forecasting + DEI-per-funnel-stage + quality-of-hire surveys
- [ ] Assessments integration hook (HackerRank/CodeSignal-class) on the pipeline
- [ ] Multi-step offer/req approval routing + real e-sign provider + background-check hook
- [ ] Public API keys + outbound webhook subscriptions + a partner-integration catalog
- [ ] Adverse-impact / fairness auditing over hiring outcomes (extends blind screening + explainable matches)
- [ ] Career pathing / opportunity recommendations for talent (aligns with the marketplace positioning; reuses matching)
- [ ] Programmatic board multiposting / Apply Connect `[DECISION: board API credentials]`
- [ ] Enterprise SSO (SAML) + SCIM provisioning
- [ ] Conversational apply / screening chatbot + autonomous agents `[DECISION: LLM egress]`

**Tier 3 — strategic forks / out of current scope (need a product decision, not a parity gap):**
- **Marketplace transaction layer** (Upwork model): payments/escrow, milestones, contracts, two-sided
  reviews/reputation, worker classification/EOR. A large regulatory + product commitment — the biggest fork
  for whether Illumin360 becomes *transactional* vs. staying sourcing/ATS + marketplace-matching.
- **Contingent-workforce back-office** (Bullhorn/VNDLY): VMS, pay-bill, timesheets, redeployment. Staffing-agency breadth, adjacent to the excluded HRMS/payroll set.
- **Interview recording/transcription intelligence** (Notetaker/Pillar): media capture + AI — heavy, and privacy-sensitive.
- **Native mobile app** — still a separate product decision.
