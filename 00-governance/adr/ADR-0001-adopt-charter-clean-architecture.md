# ADR-0001: Adopt the AI Project Bootstrap Charter & Clean Architecture

- **Status:** Accepted
- **Date:** 2026-05-29
- **Deciders:** Uriel Tapiwa Munjanga (Lead Engineer/Architect)

## Context
Illumin360 has a rich SDLC document set but no engineering codebase. We need a binding standard so the
platform is production-grade, observable, secure, and consistent across many bounded contexts and five portals.

## Decision
Adopt `CHARTER.md` as the binding engineering standard. All backend services and BFFs follow **Clean
Architecture** (Domain → Application → Infrastructure → Presentation; dependencies inward only), built as
independently deployable **microservices** with database-per-service.

## Consequences
**Positive:** Consistency, testability, framework independence in the domain, clear ports & adapters.
**Negative:** More projects/boilerplate per service; mitigated by the Candidates reference vertical slice and templates.

## Alternatives considered
1. Layered N-tier monolith — rejected: poor independent deployability and bounded-context isolation.
2. Transaction-script services — rejected: weak domain modelling for matching/compliance logic.
