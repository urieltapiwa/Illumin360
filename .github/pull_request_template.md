## What & why
<Summary + linked issue/story. Figma frame for UI work.>

## Definition of Done (charter Part 19)
- [ ] Clean Architecture respected; analyzers/format pass; warnings-as-errors clean
- [ ] Unit + integration + contract tests green; coverage gate met
- [ ] OpenAPI/.proto updated & backwards-compatible
- [ ] `/health/{live,ready,startup}` cover new dependencies
- [ ] OTel traces/metrics/logs visible in Grafana; dashboard + alerts updated
- [ ] AuthN/AuthZ enforced (gateway + BFF + service); Keycloak roles mapped
- [ ] Security scans pass (no high/critical); secrets externalised; audit logging for mutations
- [ ] XML docs + README/runbook/ADR/changelog updated; DocFX builds
- [ ] Dockerfile builds non-root image; Compose/Helm updated; CI green; deploys to dev with passing smoke+health
- [ ] Accessibility checked (WCAG 2.2 AA) for UI; Figma frame linked
