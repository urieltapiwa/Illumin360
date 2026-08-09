# Security Policy (engineering)

> **Status:** Accepted · **Owner:** Platform Architecture · **Last updated:** 2026-05-29 · **Related ADRs:** —

Implements charter Part 9. See also root `SECURITY.md` and `03-architecture/security-architecture.md`.

- **Secrets:** HashiCorp Vault only. No plaintext secrets in source; `gitleaks` gates CI. Dev defaults in
  `deploy/docker/.env.example` are non-production and clearly marked.
- **Dependencies:** central package management; CycloneDX SBOM per build; Trivy scan; Renovate updates; signed images.
- **AuthN/AuthZ:** Keycloak (ADR-0003). Enforce at gateway (coarse), BFF (session/role), service (resource-level).
- **Crypto:** TLS 1.2+ in transit (1.3 preferred); AES-256 at rest for `id_number`/`student_number` via pgcrypto.
- **Uploads:** ClamAV scan before storage. **Audit:** immutable logs for every Staff/Admin/Support mutation.
- **Pipeline gates:** CodeQL (SAST), Trivy (SCA), gitleaks, OWASP ZAP (DAST) — fail on high/critical.
