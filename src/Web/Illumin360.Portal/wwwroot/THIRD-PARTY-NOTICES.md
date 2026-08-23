# Third-party assets (self-hosted)

These vendored assets are served locally (no CDN) so the portal's strict
Content-Security-Policy (`default-src 'self'`) holds. Licenses retained below.

## HopeUI (Bootstrap 5 admin design system)
- Files: `hopeui/css/hope-ui.min.css`, `hopeui/images/loader.gif`
- Source: https://github.com/iqonicdesignofficial/hope-ui-design-system
- License: **MIT** © Iqonic Design
- Local modification: the external `@import` of Google Fonts was removed from
  `hope-ui.min.css` (Inter is self-hosted instead, see below) so no external
  request is made.

## Fonts
- **Inter** — `hopeui/fonts/inter-*.woff2` — SIL Open Font License 1.1 (Google Fonts). HopeUI's default typeface.
- **Bricolage Grotesque**, **Hanken Grotesk**, **JetBrains Mono** — `fonts/*.woff2` —
  SIL Open Font License 1.1 (Google Fonts). Illumin360's own brand faces (used by the pre-HopeUI design; retained).

All woff2 files are the Latin subsets fetched from Google Fonts and stored locally.
