# ADR-0010: Realm-per-portal identity isolation

- **Status:** Accepted
- **Date:** 2026-08-23
- **Deciders:** Platform engineering

## Context
The six MVC portals (Admin, Business, Student, Professional, Employer, Support) originally
authenticated against a single shared Keycloak realm (`illumin360`). Because every portal client lived
in one realm, a single sign-in produced a browser SSO session that silently granted access to **all
six** portals — an admin login could open the student portal and vice-versa. Distinct user populations
(platform admins, support agents, students, professionals, companies, employers) should not share a
sign-on boundary, and a compromised or over-scoped session in one portal should not reach the others.

## Decision
Give each portal its **own Keycloak realm**, so authentication is isolated per portal and there is no
cross-portal SSO:

| Realm | Portal | Roles |
|-------|--------|-------|
| `admin` | admin-web | admin.read/write/superuser |
| `business` | business-web | client.user |
| `student` | student-web | client.user, client.student |
| `professional` | professional-web | client.user |
| `employer` | employer-web | client.employer, client.user |
| `support` | support-web | support.l1/l2/lead |

The Admin portal uses a **dedicated `admin` realm** — deliberately **not** Keycloak's `master` realm,
which is reserved for Keycloak's own administration and would hand app admins super-admin surface over
every realm and the console.

Because the gateway fans a single portal's calls out to shared microservices (e.g. the Business portal
calls `/api/admin/talent-insights`), **every microservice trusts all six portal realms** (plus the
legacy `illumin360` realm). `AddIllumin360Auth` registers one JWT bearer handler per realm and a policy
scheme that forwards each request to the handler matching the token's `iss`; realm roles are projected
into ASP.NET role claims exactly as before.

The legacy `illumin360` realm is retained for the old business SPA/BFF and the gateway client, so
nothing outside the six MVC portals changes.

Realms are declared as version-controlled exports under `deploy/keycloak/realms/` and provisioned by
`deploy/keycloak/provision-realms.sh` (admin REST API, no Keycloak restart). The four self-registration
realms each get an `illumin360-registration` service client whose service account holds
`realm-management` (manage-users/view-users/query-users) plus the realm roles it assigns.

## Consequences
**Positive:**
- True isolation: one portal's session cannot open another portal. Blast radius of a compromised
  session is one realm.
- Per-portal policy: password rules, brute-force, token lifetimes, and self-registration are tunable
  per audience.
- Admin login is off the `master` realm — no accidental Keycloak super-admin exposure.

**Negative / trade-offs:**
- A person who is genuinely two things (e.g. a student who is also an employer) needs two accounts.
- Every service now maintains JWKS/discovery for seven realms and does an issuer lookup per request
  (cheap; keys cached per handler).
- More Keycloak objects to provision and keep consistent (mitigated by the checked-in realm exports +
  idempotent provisioning script).

## Alternatives considered
1. **Keep one realm, stop cross-portal SSO** (per-client sessions / force re-auth) — rejected: not true
   isolation; users/roles still share one boundary and one breach domain.
2. **Realm-per-portal for login only, one resource realm for APIs via identity brokering / token
   exchange** — rejected: more moving parts in Keycloak (brokers, exchange grants) for less isolation
   benefit than trusting the six issuers directly at the service layer.
3. **Use the `master` realm for Admin** (as first sketched) — rejected: `master` controls all realms
   and the Keycloak console; app admins do not belong there.
