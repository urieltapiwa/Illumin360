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
| `Oidc:Authority` | `http://localhost:8080/realms/illumin360` | **back-channel** issuer URL the BFF resolves internally (`http://keycloak:8080/realms/illumin360`) |
| `Oidc:FrontChannelAuthority` | _(unset — both channels are localhost)_ | **front-channel** URL the browser must reach (`http://localhost:8080/realms/illumin360`) |
| `Oidc:ClientId` | `illumin360-business-bff` | same |
| `Oidc:ClientSecret` | via env `Oidc__ClientSecret` (never commit; prod → Vault) | Vault |
| `ReverseProxy:Clusters:gateway` | `http://localhost:8088/` | `http://gateway:8080/` |
| `ReverseProxy:Clusters:spa` | `http://localhost:5173/` (vite) | the SPA container |

### Keycloak hostname alignment (Docker)
In Docker the BFF resolves Keycloak by its in-network name (`keycloak:8080`) while the user's browser can only
reach the published port (`localhost:8080`). `Oidc:Authority` stays on the **back-channel** host so discovery,
the code→token exchange and JWKS retrieval are internal; `Oidc:FrontChannelAuthority` rewrites **only the
authorize + end-session redirects** (the URLs the browser follows) to the browser-reachable host, in
`OnRedirectToIdentityProvider` / `…ForSignOut`. Two consequences of the split, both handled in `Program.cs`:

- **Token issuer.** Keycloak (per-request issuer derivation) stamps the token `iss` with the host the user
  *authenticated* against — the **front-channel** host — not the back-channel discovery host. So both are added
  to `TokenValidationParameters.ValidIssuers`; the signing keys still come from the back-channel JWKS.
- **Claims source.** `GetClaimsFromUserInfoEndpoint = false` — the back-channel user-info endpoint rejects a
  token minted for the front-channel host, so identity claims are read from the (already-validated) id_token.

This deliberately leaves the **shared** dev Keycloak on per-request issuer derivation (no fixed `KC_HOSTNAME`);
pinning one would rewrite the issuer for the sibling apps (SalesApp / StoreCatalogue) and break their
back-channel validation. Local dev leaves `FrontChannelAuthority` unset (both channels are already `localhost`)
→ no rewrite. Verified end-to-end: demo user → KC login at `localhost:8080` → callback → cookie session →
token-relayed `/api` returning live data. (The gateway and resource APIs do not themselves validate the bearer
token today, so the relayed token's `iss` does not affect `/api`; the BFF's `authenticated` route policy is
what gates it.)

PAR (Pushed Authorization Requests) is currently `Disable`d (code+PKCE is the baseline). Re-enable
`PushedAuthorizationBehavior = UseIfAvailable` once the confidential client + KC PAR are confirmed.

## Keycloak client (one-time)
The confidential client `illumin360-business-bff` is declared in `deploy/keycloak/illumin360-realm.json`
(secret `bff-dev-secret-local-only`, redirect `http://localhost:5180/bff/signin-callback`). The **shared** dev
Keycloak must have this client for the full flow to work — create it from the realm file, the admin console,
or the admin REST API. Until then `/bff/login` builds the correct redirect but Keycloak rejects the unknown client.
The client's redirect URIs must include the BFF's external URL (`http://localhost:5180/bff/signin-callback`).

## Run locally
`Oidc__ClientSecret=bff-dev-secret-local-only ASPNETCORE_ENVIRONMENT=Development dotnet run` (listens on 5180).
Point the SPA at it by serving the SPA behind the BFF and building it with `VITE_USE_BFF=1`.

## Probes
- `/health/live`, `/health/ready` (self-check; no downstream dependencies are gated).

## SPA integration
`web/business-portal/src/auth.ts` switches to BFF mode when built with `VITE_USE_BFF=1`: `initAuth` calls
`/bff/user`, `login()` navigates to `/bff/login`, `logout()` POSTs `/bff/logout`. Default (flag unset) keeps the
existing public-client keycloak-js flow so the current demo is unaffected.
