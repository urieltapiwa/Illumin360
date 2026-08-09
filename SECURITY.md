# Security Policy

## Reporting a vulnerability
Email **security@illumininvestments.com** with details and reproduction steps. Do **not** open a public
issue for security reports. We aim to acknowledge within **2 business days** and provide a remediation
timeline after triage.

## Standards
- **Baseline:** OWASP ASVS. Defend against OWASP Top 10 + API Security Top 10.
- **IAM:** Keycloak (OIDC/OAuth 2.1). Access tokens 15-min expiry, refresh tokens 30-day. MFA mandatory for
  `admin` and `support` realms. Passwords owned by Keycloak (argon2id) — the platform never stores passwords.
- **Secrets:** HashiCorp Vault (OSS). No plaintext secrets in source. `gitleaks` runs in CI.
- **Supply chain:** pinned dependencies (central package management), CycloneDX SBOM per build, Trivy scans,
  Renovate/Dependabot updates, signed images.
- **Transport:** TLS 1.2+ everywhere (1.3 preferred), HSTS. mTLS for service-to-service (mesh optional).
- **At rest:** encrypted DB volumes; `id_number` / `student_number` AES-256 via PostgreSQL pgcrypto.
- **Uploads:** ClamAV virus scanning on every uploaded file before storage.
- **Audit:** every Staff/Admin/Support mutating action logged immutably (actor, time, before/after, correlation id).

## Pipeline gates
SAST (Roslyn analyzers + CodeQL), SCA (Trivy), secret scan (gitleaks), DAST (OWASP ZAP against staging).
Builds fail on high/critical findings.

See also: `03-architecture/security-architecture.md`, `00-governance/policies/security-policy.md`,
and the canonical `09_Compliance_Legal/` document set.
