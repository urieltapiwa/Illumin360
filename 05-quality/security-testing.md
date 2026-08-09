# Security Testing Cadence

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-05-29 · **Related ADRs:** —

SAST (CodeQL + analyzers) every PR; SCA (Trivy) every PR + nightly; secret scan (gitleaks) every PR;
DAST (OWASP ZAP) against staging weekly + pre-release; dependency updates via Renovate. Fail builds on
high/critical. See `.github/workflows/ci.yml`.
