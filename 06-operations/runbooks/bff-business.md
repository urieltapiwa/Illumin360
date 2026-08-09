# Runbook — Business BFF

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-06-11 · **Related ADRs:** ADR-0003 (Keycloak)

**Purpose:** Backend-For-Frontend for the Business portal SPA. Runs the OIDC authorization-code (+ PKCE) flow
against Keycloak **server-side**, holds the resulting access/refresh/id tokens in an encrypted HTTP-only cookie
session, and reverse-proxies `/api/**` to the gateway with the user's access token attached (**token relay**).
The browser never sees a token (charter Part 2/7). **Port:** 8080 (container) / 5180 (local). **No database.**

## How it works
- `GET /bff/login?returnUrl=/path` → 302 to Keycloak authorize (code + PKCE/S256, `response_mode=form_post`).
- `GET /bff/signin-callback` → code→token exchange (confidential client), establishes the cookie session.
- `GET /bff/user` → `{ authenticated, name, email, company }` (no tokens) — the SPA polls this on load.
- `POST /bff/logout` → clears the cookie session (+ OIDC end-session).
- `/api/**` → YARP to the gateway (`/api → /v1` is done at the gateway) with `Authorization: Bearer <access_token>`;
  unauthenticated calls get **401** (the SPA then sends the user to `/bff/login`). Everything else proxies to the SPA.

## Configuration (`Oidc` section + env)
| Key | Local dev | Docker |
| --- | --- | --- |
| `Oidc:Authority` | `http://localhost:8080/realms/illumin360` | Keycloak issuer URL reachable by the BFF |
| `Oidc:ClientId` | `illumin360-business-bff` | same |
| `Oidc:ClientSecret` | via env `Oidc__ClientSecret` (never commit; prod → Vault) | Vault |
| `ReverseProxy:Clusters:gateway` | `http://localhost:8088/` | `http://gateway:8080/` |
| `ReverseProxy:Clusters:spa` | `http://localhost:5173/` (vite) | the SPA container |

PAR (Pushed Authorization Requests) is currently `Disable`d (code+PKCE is the baseline). Re-enable
`PushedAuthorizationBehavior = UseIfAvailable` once the confidential client + KC PAR are confirmed.

## Keycloak client (one-time)
The confidential client `illumin360-business-bff` is declared in `deploy/keycloak/illumin360-realm.json`
(secret `bff-dev-secret-local-only`, redirect `http://localhost:5180/bff/signin-callback`). The **shared** dev
Keycloak must have this client for the full flow to work — create it from the realm file, the admin console,
or the admin REST API. Until then `/bff/login` builds the correct redirect but Keycloak rejects the unknown client.

## Run locally
`Oidc__ClientSecret=bff-dev-secret-local-only ASPNETCORE_ENVIRONMENT=Development dotnet run` (listens on 5180).
Point the SPA at it by serving the SPA behind the BFF and building it with `VITE_USE_BFF=1`.

## Probes
- `/health/live`, `/health/ready` (self-check; no downstream dependencies are gated).

## SPA integration
`web/business-portal/src/auth.ts` switches to BFF mode when built with `VITE_USE_BFF=1`: `initAuth` calls
`/bff/user`, `login()` navigates to `/bff/login`, `logout()` POSTs `/bff/logout`. Default (flag unset) keeps the
existing public-client keycloak-js flow so the current demo is unaffected.
