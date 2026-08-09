# Data Retention & Privacy Policy

> **Status:** Accepted · **Owner:** Platform Architecture · **Last updated:** 2026-05-29 · **Related ADRs:** —

Aligns with the canonical `09_Compliance_Legal/` set (Data Protection, Labour Act, PCI-DSS, retention notices).

- Classify data (public / internal / PII / payment). Log all PII access.
- Retention: per data class; right-to-erasure honoured (GDPR-equivalent). Soft-delete where audit requires.
- Payments: PCI-DSS scope minimised — no PAN stored; tokenised via the payment gateway.
- Backups: daily PostgreSQL, 30-day retention; restore tested and documented in runbooks.
