# Third-party assets (self-hosted)

Served locally (no CDN) so the app makes no external requests. Licenses retained.

## HopeUI (Bootstrap 5 admin design system)
- CSS: `css/core/libs.min.css`, `css/hope-ui.min.css`, `css/custom.min.css`
- JS: `js/libs.min.js`, `js/external.min.js` (bundles ApexCharts), `js/hope-ui.js`
- Image: `images/loader.gif`
- Source: https://github.com/iqonicdesignofficial/hope-ui-html-admin-dashboard
- License: **MIT** © Iqonic Design
- Local modification: the external Google-Fonts `@import` was removed from the CSS
  (Inter is self-hosted below) so no external request is made.

## Inter font
- `fonts/inter-*.woff2` — SIL Open Font License 1.1 (Google Fonts, Latin subset). HopeUI's default typeface.

`brand.css` and `js/theme.js` are Illumin360's own (the HopeUI-blue ⇄ Illumin360-green theme toggle).
