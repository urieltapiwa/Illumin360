# ADR-0009: Adopt server-rendered Blazor for the portal (retire the React SPA)

- **Status:** Accepted
- **Date:** 2026-08-14
- **Deciders:** Product owner; engineering
- **Accepted:** 2026-08-14 — pilot validated (see "Pilot results"); migration to proceed portal-by-portal.

## Context

The user-facing `web/business-portal` is a **React SPA** (Vite) served as static assets by
nginx and fronted by the **Business BFF** (`Illumin360.Bff.Business`), which does OIDC
(code + PKCE) against Keycloak, keeps tokens in an encrypted server-side cookie, and
token-relays `/api/*` to the YARP gateway.

The product owner is dissatisfied with the SPA on four counts, all confirmed in the code:

1. **Bundle / performance** — one client bundle of **~1.85 MB** (the Vite build emits a
   >500 kB warning); everything renders client-side.
2. **Routing / deep-linking** — client-side `?portal=…` routing; each role is **one giant
   scrolling page** (the reason we recently had to add scrollspy/anchor navigation instead
   of real routes); weak back-button and URL semantics.
3. **Client complexity** — React state, i18n, theming, and a **demo/live data split** all
   live in the browser.
4. **No real server-rendered pages** — nothing is indexable or addressable per URL.

Security is an explicit priority. The backend is **.NET 10** microservices behind a YARP
gateway; authentication is already handled server-side by the BFF.

## Decision

Replace the React SPA with a **Blazor Web App (.NET 10)** using **static server-side
rendering (SSR)** by default, enabling **interactive Server** render mode only on the
pages that genuinely need live interactivity (dashboards).

The Blazor app **subsumes the Business BFF role**: it performs OIDC (code + PKCE) against
Keycloak, holds access/refresh tokens server-side in the encrypted auth cookie, and calls
the gateway **server-side** with a token-relay `HttpClient`. The browser receives HTML and
an HttpOnly session cookie only — **never a token**.

Migration is **phased, portal-by-portal**; the React SPA and the standalone BFF are retired
once the last portal is moved.

## Consequences

**Positive**
- **Near-zero JavaScript** by default (static SSR); no multi-MB bundle; fast first paint.
- **Real per-URL routes** (`/employer`, `/employer/team`, `/student/applications`), native
  back-button, bookmarking, deep-linking — the scrollspy workaround becomes unnecessary.
- **Logic in C# on the server** — one language shared with the backend; the demo/live split
  disappears (pages render real data or an empty/error state).
- **Server-rendered, indexable HTML** per route.
- **Stronger security posture:** the access token never enters the browser; the
  dependency/supply-chain surface shrinks dramatically (the npm tree — echarts,
  framer-motion, i18next, … — is removed in favour of one .NET runtime); Content-Security-
  Policy becomes trivial to lock down; antiforgery is built into Blazor SSR form handling.

**Negative / trade-offs**
- **Large migration effort** — a full presentation-layer rewrite (5 role portals + the
  business dashboard, charts, EN/AF i18n, theming). Done in phases, not big-bang.
- **Interactive-Server pages** hold a per-user SignalR **circuit** (server memory + affinity
  considerations at scale); mitigated by using static SSR for everything except genuinely
  interactive views.
- **Charts** must be re-approached — echarts/React components go away. Options: server-
  rendered SVG for simple visuals; a small JS-interop charting lib loaded only on chart
  pages. Decided concretely during the pilot.
- **Two frontends coexist** during migration (extra routing/config until cutover).

## Alternatives considered

1. **Next.js / Remix (React SSR/MPA)** — rejected: adds a Node runtime and **retains the
   large npm dependency surface**; weaker on the "one language / minimal client / smallest
   attack surface" goals than staying in .NET.
2. **Razor Pages / MVC + HTMX** — viable and even leaner on JS, but Blazor handles the
   interactive dashboards better without hand-rolled JavaScript. Kept as the fallback for
   simple, non-interactive pages.
3. **Keep the SPA; code-split + add real client routes** — rejected: does not address
   client-side token exposure, client complexity, or SSR/SEO; treats symptoms only.

---

## Design detail (for the pilot and beyond)

### Project layout
```
src/Web/Illumin360.Portal/            # new Blazor Web App (.NET 10)
  Program.cs                          # OIDC + cookie + token-relay HttpClient + render modes
  Components/
    App.razor, Routes.razor, _Imports.razor
    Layout/PortalLayout.razor         # shared sidebar shell (real <NavLink> routes)
    Pages/Employer/
      Profile.razor      (@page "/employer")
      Team.razor         (@page "/employer/team")
  Services/
    EmployersApiClient.cs             # typed client -> gateway /v1/employers
    TokenRelayHandler.cs              # attaches the signed-in user's access_token
  appsettings.json, Dockerfile
```

### Auth / token flow (security core)
- Reuse the BFF's proven Keycloak wiring **verbatim**: cookie scheme (`illumin360.portal`,
  HttpOnly), OIDC code + PKCE, `SaveTokens = true`, `GetClaimsFromUserInfoEndpoint = false`,
  and the **front/back-channel authority alignment** (`Authority = keycloak:8080`,
  redirects rewritten to `localhost:8080`, both issuers accepted).
- A `TokenRelayHandler : DelegatingHandler` pulls `access_token` via
  `IHttpContextAccessor` → `HttpContext.GetTokenAsync("access_token")` and sets the Bearer
  header on every gateway call. **Tokens stay on the server.**
- Register a **new confidential Keycloak client** `illumin360-portal` (mirrors
  `illumin360-business-bff`), or reuse the existing client with an added redirect URI during
  the pilot.

### Routing map (replaces `?portal=`)
`/employer` · `/employer/team` · `/employer/candidates` · `/student` · `/student/applications`
· `/admin/...` — real routes rendered by `@page`, guarded by `[Authorize(Policy = …)]`
matching the API policies (`student`, `professional`, `admin.write`).

### Charting
Employer pilot is table/form-only (no charts) — deliberately chosen so the pilot proves the
core SSR + auth + data-write path first. Chart strategy (server-SVG vs. minimal JS-interop
island) is decided as a dedicated step before the dashboard-heavy portals (Admin/Business).

### Deployment / coexistence
- New container `portal` in `docker-compose.apps.yml`, on the `illumin360` network, calling
  `http://gateway:8080` server-side; published on a distinct port during the pilot.
- The SPA + BFF keep running unchanged; only migrated routes point at Blazor. Final cutover
  retires `web` (nginx SPA) and `Illumin360.Bff.Business`.

### Migration order (largest last)
Employer (pilot) → Support → Student → Professional → Business dashboard → Admin → retire SPA/BFF.

---

## Standards conformance

The migration is built against the organisation's standards baseline. How each applies:

| Standard | How it is addressed |
|---|---|
| **OWASP Top 10 / ASVS** | Server-side session (A01/A07); access token never in the browser (A02); antiforgery on all SSR form posts (A01/CSRF); Razor output auto-encoding (A03/XSS); input validation via DataAnnotations (A03); security response headers — CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy (A05, ASVS V14.4); open-redirect guard on login/logout return URLs (A01); far smaller dependency surface than the npm SPA (A06). |
| **NIST SP 800-53** | AC (policy-gated routes matching API roles), SC-7/SC-8/SC-18 (CSP + HSTS + framing controls), IA (OIDC/PKCE via Keycloak), SI-10 (input validation). |
| **WCAG 2.2 / ISO 9241-110/-161** | Semantic landmarks; real `<label>`s bound to inputs (not placeholder-only); `fieldset`/`legend`; `aria-live` status region for async results; visible keyboard focus (2.4.7); ≥4.5:1 contrast tokens; `prefers-reduced-motion` respected; native routes give correct back-button/history (controllability, self-descriptiveness). |
| **GDPR** | Data minimisation (only the fields a page needs); no PII in logs; no third-party client scripts/trackers; internal-portal consent is out of scope for this ADR. |
| **PCI-DSS / HIPAA** | Not in scope — the portal handles no cardholder or health data. Payment flows stay server-side in the Payments/Billing services behind the gateway. |
| **ISO/IEC 27001 · SOC 2 · Cloud Well-Architected** | Organisation/process-level (ISMS, trust criteria, deployment). The technical controls above are the evidence artefacts these frameworks audit; no portal-specific decision here beyond "server-rendered reduces client attack surface." |

## Pilot results (measured, 2026-08-14)

Employer pilot (`src/Web/Illumin360.Portal`) running against the live stack:

- **Server-rendered live data:** `curl` (no JS) of `/employer` returns fully-populated HTML —
  "Namib Mills / Manufacturing / Windhoek" pulled from the live Employers API through the
  gateway, token relayed server-side. The SPA returns an empty shell by contrast.
- **JS payload:** page HTML **5.7 KB** fully rendered; framework runtime **195 KB** (cached,
  optional for static pages) vs. the SPA's **~1,851 KB** app bundle that must load before first paint.
- **Security headers** present on every response (CSP, X-Frame-Options: DENY, nosniff,
  Referrer-Policy, Permissions-Policy); build is **green under the repo's strict analyzers**
  (StyleCop + warnings-as-errors).
- **RBAC preserved:** the invite write is gated exactly as the API enforces it — anonymous → 401,
  wrong role → 403, `admin.write` → 201 + persisted (proven end-to-end earlier this session).
