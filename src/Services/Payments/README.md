# Payments Service

Marketplace **transaction layer** — fixed-price **contracts**, **milestones** (fund → submit → approve/refund),
and an append-only **ledger**. Money movement runs through the `IPaymentProvider` port; the domain/ledger stay
provider-agnostic. See [`03-architecture/marketplace-transactions-design.md`](../../../03-architecture/marketplace-transactions-design.md).

> **Status:** Phase 1 (contracts & milestones as agreements). Default `IPaymentProvider` is the deterministic
> **`FakePaymentProvider`** — **no real money**. Four real adapters are **scaffolded** behind the port and
> config-selectable via `Payments:Provider` (D1 resolved: Namibia/SADC first, **Flutterwave** recommended):
> `Flutterwave` + `Stripe` (tested reference pair) and `NGenius` + `Dpo` (structured scaffolds — validate
> against each provider's sandbox before enabling). A real adapter is used only when `Provider` names one,
> `Enabled=true`, and a `BaseUrl` is set — **and going live still requires the D2 legal sign-off + credentials.**

## Provider config (`Payments` section, default off)
```jsonc
"Payments": { "Provider": "Fake", "Enabled": false, "BaseUrl": "", "SecretKey": "", "Extra": "" }
```
`Extra` carries provider-specifics (N-Genius outlet reference; DPO company token). **Port gap:** Release/Refund
take only `(idempotencyKey, holdReference)` — capture/refund-by-id PSPs (Stripe) fit; transfer-to-destination
PSPs (Flutterwave/N-Genius/DPO payouts) need a destination-account + amount port extension before go-live.

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

## Portal UI
`web/business-portal/src/Contracts.tsx` — a shared "Contracts & escrow" panel: the **Employer** portal drives
the client side (draft a contract, add milestones, activate, fund, approve/refund) and the **Professional**
portal is the talent side (view contracts, submit funded milestones). Both show the ledger.

## Not built yet (gated / later phases)
- **Real PSP adapter** (Phase 2, decision D1) + provider-hosted funding + webhooks + reconciliation.
- **Reviews/reputation** already shipped in the Recruitment service (Phase 0), feeding `ReputationScorer`.
- Platform take-rate split, disputes, hourly contracts (design doc D3/D5/D6).
