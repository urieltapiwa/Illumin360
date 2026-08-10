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
| Kanban drag-drop pipeline UI | ⬜ | Horilla has this |
| Bulk actions | ⬜ | |
| Application status visible to applicant | ✅ | "My applications" live status timeline (`GET /recruitment/talents/{id}/applications`) on the professional portal |

- [x] Application stage-transition endpoints — `POST /v1/recruitment/applications/{id}/advance|reject` (admin-gated), domain stage machine (applied→reviewed→shortlisted→hired) with terminal-conflict guards. Free-text reject reason pending (needs a column on the externally-seeded `applications` table)
- [ ] Recruiter pipeline board (kanban) per requisition
- [x] Applicant-facing application status timeline — "My applications" panel on the professional portal, live status per applied role (`GET /recruitment/talents/{id}/applications`)

## D. Matching / sourcing
| Feature | Status | Notes |
|---|---|---|
| Match score candidate↔role | ✅ | Real engine (`Illumin360.Matching`) computes professional match scores from city + role + skills |
| Real matching engine (skills/location weighting) | ✅ | Shared weighted engine: professional & student matches, marketplace open-role ranking, and employer top-candidates (`GET /candidates/top`) |
| Personalized recommendations | ✅ | Professional matches and marketplace open roles both ranked by engine score (`/me/role-scores`) |
| Saved searches | ⬜ | |
| Job alerts / email digests | ⬜ | |
| Talent pools / shortlists | ⬜ | |

- [x] Matching engine (weighted city + role + skills) producing real scores — shared `Illumin360.Matching`, applied to **professional & student** matches (ranked by score) and the professional marketplace panel
- [x] "Recommended roles for you" — marketplace open roles ranked per professional (`POST /me/role-scores`, match % shown/sorted) — and the employer flip side, "top candidates for a role" (`GET /v1/candidates/top?title=&city=`)
- [ ] Saved searches + job alert digests
- [ ] Shortlists / talent pools

## E. Interviews & scheduling
| Feature | Status | Notes |
|---|---|---|
| Interview scheduling | ⬜ | |
| Calendar integration (ICS/Google) | ⬜ | |
| Interview scorecards / feedback | ⬜ | |
| Panel interviews | ⬜ | |

- [ ] Schedule interview (slot, attendees, ICS invite)
- [ ] Interview scorecard + feedback capture

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
| Transactional email (templated) | ✅ | Shared `Illumin360.Email` (MailKit/SMTP → Mailpit) + templates; Notifications worker sends a welcome email on registration. Application-event emails pending |
| In-app notification center | ⬜ | |
| In-app messaging (candidate↔employer) | ⬜ | |
| Bulk email / campaigns | ⬜ | |

- [x] Email infrastructure — shared `Illumin360.Email` (MailKit SMTP → Mailpit) + templates; Notifications worker sends a **welcome email on registration** (verified end-to-end with a Testcontainers Mailpit)
- [ ] Templated emails on application received / status change (wire recruitment events → worker)
- [ ] In-app notification center
- [ ] Direct messaging between employer and candidate

## H. Employer / recruiter tooling
| Feature | Status | Notes |
|---|---|---|
| Employer self-registration (identity + role) | ✅ | via BFF `/register` |
| Employer/company profile service | ⬜ | No employers service yet |
| Multi-user employer teams + roles | ⬜ | |
| Recruiter CRM (clients/contacts) | ⬜ | OpenCATS has this |
| Branded careers page | ⬜ | |

- [ ] Employers service (company profile, members)
- [ ] Employer team roles (owner/recruiter/viewer)

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
- Total build items: 31
- Done: 9
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

_Update this file as items are ticked; link the commit/PR that delivered each._
