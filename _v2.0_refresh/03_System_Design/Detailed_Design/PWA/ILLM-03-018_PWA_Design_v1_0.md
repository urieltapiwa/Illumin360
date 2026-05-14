# Progressive Web App — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 PWA — Detailed Design |
| Document ID | ILLM-03-018_PWA_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft |
| Source authority | Section 25, Illumin360 Complete Technical Specification v3.6 |
| Phase | Phase 6 |
| Owner | Front-end Engineering |

## 1. Purpose

This document specifies the Progressive Web App layer of the Illumin360 platform — manifest, service worker, install behaviour, icon set, theme, and offline fallback. No database changes are required.

## 2. Goals

- Allow users on supported browsers to install Illumin360 to their desktop, taskbar, dock, or home screen
- Launch Illumin360 in a standalone window without browser chrome
- Apply Illumin brand theming (Illumin green `#1D9E75`) for the OS-level window decoration
- Cache static assets for instant loads and a graceful offline fallback page
- Auto-update in the background when a new version is deployed

## 3. Manifest

`manifest.json` is served at `/manifest.json` from the front-end origin and linked from the document head:

```html
<link rel="manifest" href="/manifest.json">
<meta name="theme-color" content="#1D9E75">
<link rel="apple-touch-icon" href="/icons/icon-192.png">
<meta name="apple-mobile-web-app-capable" content="yes">
<meta name="apple-mobile-web-app-status-bar-style" content="default">
<meta name="apple-mobile-web-app-title" content="Illumin360">
```

Manifest content:

```json
{
  "name": "Illumin360",
  "short_name": "Illumin360",
  "description": "Illumin360 talent matching and recruitment platform.",
  "start_url": "/app",
  "scope": "/",
  "display": "standalone",
  "orientation": "any",
  "theme_color": "#1D9E75",
  "background_color": "#FFFFFF",
  "icons": [
    {"src": "/icons/icon-72.png",  "sizes": "72x72",  "type": "image/png"},
    {"src": "/icons/icon-96.png",  "sizes": "96x96",  "type": "image/png"},
    {"src": "/icons/icon-128.png", "sizes": "128x128", "type": "image/png"},
    {"src": "/icons/icon-144.png", "sizes": "144x144", "type": "image/png"},
    {"src": "/icons/icon-152.png", "sizes": "152x152", "type": "image/png"},
    {"src": "/icons/icon-192.png", "sizes": "192x192", "type": "image/png", "purpose": "any maskable"},
    {"src": "/icons/icon-384.png", "sizes": "384x384", "type": "image/png"},
    {"src": "/icons/icon-512.png", "sizes": "512x512", "type": "image/png", "purpose": "any maskable"}
  ],
  "shortcuts": [
    {"name": "My Dashboard", "url": "/app", "icons": [{"src": "/icons/icon-96.png", "sizes": "96x96"}]},
    {"name": "Upload CV",   "url": "/app/cv-upload", "icons": [{"src": "/icons/icon-96.png", "sizes": "96x96"}]}
  ]
}
```

`description` is plain Illumin360 branding per Section 31 — no "AI" references.

## 4. Service worker

A single service worker at `/service-worker.js` handles caching and updates.

### 4.1 Caching strategy

| Resource class | Strategy |
|---|---|
| App shell (HTML, JS bundles, CSS bundles, manifest, icons) | Cache-first with version-based cache key. New deploy ⇒ new cache key ⇒ old cache evicted. |
| Static images and fonts | Cache-first with 30-day max age |
| API requests | Network-first with 3-second timeout, then cached fallback for read-only endpoints (GET only) |
| Mutations (POST/PUT/DELETE) | Always network — never cached. If offline, surface error to user. |

### 4.2 Update flow

| Event | Behaviour |
|---|---|
| New deploy detected (new service worker version) | Service worker installs in background |
| Install complete | `waiting` state. The active service worker continues to serve until the user reloads. |
| In-app prompt | A non-blocking banner appears: "A new version of Illumin360 is available. **Reload to update.**" |
| User clicks reload | `skipWaiting` + `clients.claim` activates the new worker; page reloads |

### 4.3 Offline fallback

When the user is offline and requests a page not in cache, the service worker serves `/offline.html` — a static page that:
- Explains the user is offline
- Shows the Illumin logo and brand colour
- Lists the cached actions still possible (view profile, view recent shortlist preview if cached)
- Does not contain any reference to AI or third-party providers

## 5. Install behaviour

| Platform | Behaviour |
|---|---|
| Chrome / Edge (desktop and Android) | Native install prompt after engagement criteria met (multiple visits over 30 days). The platform listens for `beforeinstallprompt` and exposes an "Install Illumin360" button on the dashboard. |
| Safari (macOS) | Sonoma+ supports PWA add-to-dock via the share menu. No programmatic prompt — surfaced via help tooltip on first session. |
| Safari (iOS / iPadOS) | Add to Home Screen via share sheet. Apple meta tags ensure correct title and status bar behaviour. |
| Firefox (desktop) | Limited support — manifest is honoured for standalone display where available. |

The `Install Illumin360` button is displayed in the user dashboard top-right menu when `beforeinstallprompt` is captured. When clicked, the captured prompt is shown; on accept, the button hides permanently for that browser.

## 6. JWT handling on launch

When the PWA launches:

1. Service worker activates immediately.
2. Document loads `start_url` = `/app`.
3. Client checks for stored JWT in IndexedDB (preferred over localStorage for service-worker access).
4. If valid and not expired → render dashboard.
5. If expired → attempt refresh via the refresh token (if present and valid).
6. If no JWT or refresh fails → redirect to `/login`.

Tokens stored in IndexedDB are scoped to the origin and not exposed to other origins. The platform never stores plaintext credentials.

## 7. Push notifications (deferred)

Push notification subscription is **not** included in Phase 6. The infrastructure (service worker `push` event handler) is included as a no-op so future enabling does not require a new service-worker version. Notification permissions are not requested in Phase 6.

## 8. Telemetry

PWA-specific events logged:

| Event | Logged when |
|---|---|
| pwa.install.prompted | beforeinstallprompt captured |
| pwa.install.shown | User opened the install dialogue |
| pwa.install.accepted | User confirmed install |
| pwa.install.dismissed | User dismissed install |
| pwa.launch.standalone | Launched in `display-mode: standalone` |
| pwa.offline.fallback | User served the offline page |
| pwa.update.banner.shown | Reload-to-update banner displayed |
| pwa.update.banner.accepted | User reloaded to update |

Metrics surface in the admin analytics dashboard.

## 9. Acceptance criteria

1. Manifest validates against the W3C App Manifest spec.
2. Service worker registers successfully on the first page load and caches the app shell.
3. The "Install Illumin360" button appears on Chrome desktop after the third visit within 30 days (engagement heuristic).
4. After install, launching from the desktop / dock / home screen opens Illumin360 with no browser chrome.
5. Offline mode serves `/offline.html` for unknown routes and remains functional for cached read-only routes.
6. Mutation endpoints (POST/PUT/DELETE) fail gracefully when offline with a user-friendly message.
7. A new deploy triggers the reload-to-update banner without forcing a full page reload.
8. iOS users can add to home screen via the share menu and see Illumin branding in the splash screen.
9. No PWA copy mentions AI or third-party providers.

## 10. Cross-references

| Document | Section |
|---|---|
| Spec v3.6 | Section 25 (canonical), Section 17.1 (HTTPS required) |
| Security Design (ILLM-03-007 v2.0) | Token storage on the PWA |
| Branding Component Library (ILLM-12-008 v2.0) | Icon set and theming |
| Deployment Plan (ILLM-10-001 v2.0) | Service worker versioning at deploy |

## 11. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. |
