# ADR-0006: Exception to open-source-only — AI/ML SaaS dependencies

- **Status:** Accepted
- **Date:** 2026-05-29

## Context
Charter Principle 1 mandates open-source-only. Illumin360's product design (spec §7/§29 + Third-Party
Integrations) depends on **Anthropic Claude API** (matching narratives, AI Assistant) and **Google Cloud
Vision** (CV/document parsing) — both proprietary SaaS.

## Decision
Permit these two as **explicit, documented exceptions**, isolated behind ports in the Recruitment/AiAssistant
Infrastructure layers so they can be swapped for OSS models (e.g. self-hosted LLM, Tesseract/PaddleOCR) without
domain changes. Record provider, data-flow, and PII exposure here and in the data/security architecture.

## Consequences
**Positive:** Best-in-class AI now; clean seam for future OSS substitution.
**Negative:** SaaS cost + data-residency/PII review required; tracked in compliance + `06-operations/slo-sli.md`.

## Alternatives considered
1. Self-host OSS models day one — deferred: higher ops/quality risk pre-launch.
