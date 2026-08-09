# Security Architecture (STRIDE)

> **Status:** Draft · **Owner:** Platform Architecture · **Last updated:** 2026-05-29 · **Related ADRs:** ADR-0001..0006

Baseline OWASP ASVS; defend OWASP Top 10 + API Top 10 (charter Part 9). Per-service STRIDE threat model below
(seed — expand per service before build).

| Threat | Example | Control |
| --- | --- | --- |
| **S**poofing | Forged caller identity | Keycloak OIDC; mTLS service-to-service; token audience scoping |
| **T**ampering | Mutating request/data | Input validation (FluentValidation), parameterised queries, signed webhooks |
| **R**epudiation | Denying an action | Immutable audit logs (actor, time, before/after, correlation id) |
| **I**nfo disclosure | PII leakage | Field-level AES-256, least-privilege scopes, Support data scoped to active ticket |
| **D**oS | Flooding endpoints | Rate limiting (gateway + BFF), bulkheads, queue-depth alerts |
| **E**levation | Privilege escalation | 3-layer authz (gateway/BFF/service); MFA for admin/support; no Support financial authority |

Trust boundaries: Internet→Gateway, Gateway→Services, Service→DB/broker, Platform→external SaaS.
