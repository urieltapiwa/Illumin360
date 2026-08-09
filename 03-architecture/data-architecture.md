# Data Architecture

> **Status:** Draft · **Owner:** Platform Architecture · **Last updated:** 2026-05-30 · **Related ADRs:** ADR-0001..0008

**Database-per-service** on PostgreSQL 17 (charter Part 13). Each service owns `illumin360_<service>`.
No cross-service joins; data shared only via API/events. EF Core 10 code-first migrations applied at startup
via `MigrateAsync` (recorded in `__EFMigrationsHistory`), gated by `/health/startup`. Redis for
cache/idempotency/rate-limits/locks. Field-level AES-256 for `id_number`/`student_number` (pgcrypto).
Canonical legacy schema reference: `Illumin360 Draft Concept/illumin360_master_migrations_v3.6.sql` — being
decomposed into per-service schemas.

A service that **publishes** integration events also owns the MassTransit outbox tables in its own database
(`OutboxMessage` / `OutboxState` / `InboxState`), created by the same migration so persist + publish stay in
one transaction (ADR-0007). Candidates' `InitialCreate` migration provisions both the `candidates` aggregate
table and these outbox tables under the `candidates` schema.

| Service | Database | Owns (examples) |
| --- | --- | --- |
| Identity | illumin360_identity | users, job_seekers, employers, support_staff (profile shells) |
| Candidates | illumin360_candidates | candidate_profiles, skills, languages, qualifications, documents, videos |
| Employers | illumin360_employers | employers, employer_badges |
| Recruitment | illumin360_recruitment | recruitment_requests, candidate_matches, shortlists, auto_application_matches, match_feedback |
| Billing | illumin360_billing | subscriptions, pricing_plans, payments, invoices, receipts, business_subscriptions |
| Notifications | illumin360_notifications | email_logs, notification_logs |
| Support | illumin360_support | support_tickets, support_messages, support_attachments, knowledge_articles |
| Engagement | illumin360_engagement | referrals, insights, spotlight_features, badges, benchmark_snapshots |
| AiAssistant | illumin360_aiassistant | assistant_conversations, ai_processing_log |
