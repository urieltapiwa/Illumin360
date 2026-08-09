# ADR-0003: Keycloak as the IAM authority

- **Status:** Accepted
- **Date:** 2026-05-29

## Context
Spec v3.7 §17.1 makes Keycloak the OIDC/OAuth2 authority for all portals; the platform must never store passwords.

## Decision
Use **Keycloak 26.6.x** (Postgres-backed), realm-per-environment, with clients for the gateway, each BFF, and
service-to-service. PKCE for browser flows via BFF; client-credentials for service-to-service. Access tokens
15-min, rotating refresh 30-day. MFA mandatory for `admin` and `support`. Realms/clients provisioned as code.

## Consequences
**Positive:** Offloads auth, MFA, brute-force protection, passkeys; emits OTel.
**Negative:** Operational ownership of an HA Keycloak + its database.
