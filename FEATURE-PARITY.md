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
| Resume parsing (skills/experience extraction) | ⬜ | EazyRecruit/OpenCATS have this |
| Candidate search (boolean / faceted) | 🟡 | City ILIKE filter only |
| Recruiter notes / private activity log | 🟡 | Read-only activity feed; no recruiter notes |
| Tags / labels | ⬜ | |
| Skill endorsements / references | ⬜ | |
| Duplicate detection | ⬜ | |

- [x] Shared object-storage building block (`Illumin360.Storage`) + Professionals CV upload/download → MinIO (verified end-to-end with a Testcontainers MinIO roundtrip)
- [x] Extend CV upload to students (self-service `/me/cv`, UI + MinIO integration test) & candidates (per-id `/{id}/cv`, admin-gated)
- [ ] Resume parsing to prefill skills/experience
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
| Pipeline stages (applied→reviewed→shortlist→interview→hire) | 🟡 | Stages exist in data/funnel; no transition API |
| Advance / reject application (with reason) | ⬜ | |
| Kanban drag-drop pipeline UI | ⬜ | Horilla has this |
| Bulk actions | ⬜ | |
| Application status visible to applicant | 🟡 | Own match status shown |

- [ ] Application stage-transition endpoints (advance/reject + reason)
- [ ] Recruiter pipeline board (kanban) per requisition
- [ ] Applicant-facing application status timeline

## D. Matching / sourcing
| Feature | Status | Notes |
|---|---|---|
| Match score candidate↔role | ✅ | Real engine (`Illumin360.Matching`) computes professional match scores from city + role + skills |
| Real matching engine (skills/location weighting) | ✅ | Shared weighted engine applied to professional & student matches + marketplace open roles; employer "top candidates" pending |
| Personalized recommendations | ✅ | Professional matches and marketplace open roles both ranked by engine score (`/me/role-scores`) |
| Saved searches | ⬜ | |
| Job alerts / email digests | ⬜ | |
| Talent pools / shortlists | ⬜ | |

- [x] Matching engine (weighted city + role + skills) producing real scores — shared `Illumin360.Matching`, applied to **professional & student** matches (ranked by score) and the professional marketplace panel
- [x] "Recommended roles for you" — marketplace open roles ranked per professional via `POST /me/role-scores` (match % shown, sorted). "Top candidates for a role" (employer side) still pending
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
| Transactional email (templated) | 🟡 | Keycloak verify email only; worker is a stub |
| In-app notification center | ⬜ | |
| In-app messaging (candidate↔employer) | ⬜ | |
| Bulk email / campaigns | ⬜ | |

- [ ] Real templated email on key events (application received, status change)
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
- Total build items: 30
- Done: 5
- In progress: 0

**Changelog of ticks**
- Admin portal tickets + accounts panels — completed the ticket Assign action (2026-08-10).
- Shared object storage + Professionals CV upload/download (MinIO), end-to-end tested (2026-08-10).
- CV upload extended to Students (self-service + UI) and Candidates (per-id, admin), MinIO-tested (2026-08-10).
- Shared matching engine (`Illumin360.Matching`) — real weighted scores + ranking on professional matches (2026-08-10).
- Marketplace open roles ranked per professional (`/me/role-scores`), match % shown + sorted (2026-08-10).
- Matching engine extended to student dashboard matches (scored + ranked) (2026-08-10).

_Update this file as items are ticked; link the commit/PR that delivered each._
