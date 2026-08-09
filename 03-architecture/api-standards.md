# API Standards

> **Status:** Draft · **Owner:** Platform Architecture · **Last updated:** 2026-05-29 · **Related ADRs:** ADR-0001..0006

Charter Part 7. OpenAPI 3.1 is the contract source of truth, published to `04-design/api-contracts/`.
REST: noun resources, correct verbs/status, **RFC 9457 Problem Details** for all errors (see
`src/BuildingBlocks/Web/ResultExtensions.cs`). Versioning via `/v1/`; never break a published contract.
Cursor pagination for large sets. `Idempotency-Key` on unsafe ops (dedupe via Redis). Rate limiting at
gateway + BFF. gRPC `.proto` versioned with the service. Propagate `traceparent` end-to-end.
