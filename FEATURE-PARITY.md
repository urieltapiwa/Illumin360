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
| Resume/CV upload & storage | ⬜ | MinIO is in the stack but unused |
| Resume parsing (skills/experience extraction) | ⬜ | EazyRecruit/OpenCATS have this |
| Candidate search (boolean / faceted) | 🟡 | City ILIKE filter only |
| Recruiter notes / private activity log | 🟡 | Read-only activity feed; no recruiter notes |
| Tags / labels | ⬜ | |
| Skill endorsements / references | ⬜ | |
| Duplicate detection | ⬜ | |

- [ ] CV/resume upload → MinIO (per candidate/student/professional)
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
| Match score candidate↔role | 🟡 | Scores seeded, no engine |
| Real matching engine (skills/location weighting) | ⬜ | TalentMatch = reference |
| Personalized recommendations | 🟡 | Flat marketplace list |
| Saved searches | ⬜ | |
| Job alerts / email digests | ⬜ | |
| Talent pools / shortlists | ⬜ | |

- [ ] Matching engine (weighted skills + city + availability) producing real scores
- [ ] "Recommended roles for you" / "top candidates for this role"
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
| Support tickets (assign/resolve) | ✅ | Backend done; portal panel pending |
| User account management (suspend/activate) | ✅ | Backend done; portal panel pending |
| Service-layer RBAC | ✅ | `Illumin360.Security` |
| Audit trail (viewable) | 🟡 | Outbox events exist; no audit UI |
| GDPR data export / delete | ⬜ | |

- [ ] Admin portal panels for tickets + accounts (wire to existing APIs)
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
| Object storage (MinIO) | 🟡 | Provisioned, not yet used by a feature |
| Integration test coverage | ✅ | Testcontainers smoke tests + TestSupport |
| Mobile app | ⬜ | OrangeHRM has one |

---

### Progress
- Total build items: 30
- Done: 0
- In progress: 0

_Update this file as items are ticked; link the commit/PR that delivered each._
