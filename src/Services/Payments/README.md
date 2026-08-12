# Payments Service

Marketplace **transaction layer** — fixed-price **contracts**, **milestones** (fund → submit → approve/refund),
and an append-only **ledger**. Money movement runs through the `IPaymentProvider` port; the domain/ledger stay
provider-agnostic. See [`03-architecture/marketplace-transactions-design.md`](../../../03-architecture/marketplace-transactions-design.md).

> **Status:** Phase 1 (contracts & milestones as agreements). The only `IPaymentProvider` today is the
> deterministic **`FakePaymentProvider`** — it moves **no real money**. A real PSP adapter
> (Flutterwave / Stripe Connect, per decision **D1** — note Stripe isn't available in Namibia) is Phase 2,
> swapped in DI with no change to the domain or flows.

## Layers
- `Illumin360.Payments.Domain` · `Illumin360.Payments.Application` · `Illumin360.Payments.Infrastructure` · `Illumin360.Payments.Api`
- `tests/`: UnitTests · IntegrationTests (Testcontainers)

Owns its own PostgreSQL database (`illumin360_payments`), exposes `/health/{live,ready,startup}`, wires OTel via
`AddProjectObservability("payments")`, ships a chiseled non-root Dockerfile, and is fronted by the gateway at
`/api/payments/**` (port 5207).

## Money
Amounts are integer **minor units** (e.g. cents) + an **ISO-4217 currency** — never floats. Phase 1's ledger is
an append-only movement log; Phase 2 upgrades it to strict double-entry once real funds flow.

## Endpoints (v1)
- `GET/POST /v1/payments/contracts`, `GET /contracts/{id}` (with milestones + movements)
- `POST /contracts/{id}/milestones`, `/activate`, `/cancel`
- `POST /milestones/{id}/fund | submit | approve | refund`

## Not built yet (gated / later phases)
- **Real PSP adapter** (Phase 2, decision D1) + provider-hosted funding + webhooks + reconciliation.
- **Reviews/reputation** already shipped in the Recruitment service (Phase 0), feeding `ReputationScorer`.
- Portal UI for contracts/milestones (fast-follow).
- Platform take-rate split, disputes, hourly contracts (design doc D3/D5/D6).
