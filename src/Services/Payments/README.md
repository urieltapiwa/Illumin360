# Payments Service

Marketplace **transaction layer** — fixed-price **contracts**, **milestones** (fund → submit → approve/refund),
and an append-only **ledger**. Money movement runs through the `IPaymentProvider` port; the domain/ledger stay
provider-agnostic. See [`03-architecture/marketplace-transactions-design.md`](../../../03-architecture/marketplace-transactions-design.md).

> **Status:** Phase 1 (contracts & milestones as agreements). Default `IPaymentProvider` is the deterministic
> **`FakePaymentProvider`** — **no real money**. Four real adapters exist behind the port, verified against each
> provider's live docs (2026-08-12) and config-selectable via `Payments:Provider`. **Payout capability differs
> (this is the key finding):**
>
> | Provider | Collect | Refund | **Pay talent** | NAD/Namibia |
> |---|---|---|---|---|
> | Flutterwave | ✅ | ✅ | ✅ transfers/subaccounts | ❌ not supported |
> | Stripe | ✅ | ✅ | ✅ Connect transfers | ❌ Namibia excluded |
> | N-Genius | ✅ | ✅ (to payer) | ❌ **no payout API** | MEA |
> | DPO | ✅ | ✅ | ❌ **no payout API** | ✅ collection only |
>
> **N-Genius and DPO return an explicit not-supported result on `Release`** (they cannot pay a third party).
> **No provider offers a documented NAD payout rail** — so a Namibia-first marketplace that pays talent in NAD
> is not achievable on these four as documented (see the design doc §12). A real adapter runs only when
> `Provider` names one, `Enabled=true`, and `BaseUrl` is set — **and going live needs D2 legal sign-off +
> confirmed corridor + credentials.**

## Provider config (`Payments` section, default off)
```jsonc
"Payments": { "Provider": "Fake", "Enabled": false, "BaseUrl": "", "SecretKey": "", "Extra": "" }
```
`Extra` carries provider-specifics (N-Genius outlet reference; DPO company token).

## Payout accounts + the release transfer path
The port now carries `ReleaseInstruction` (hold ref + amount + currency + **destination account**) and
`RefundInstruction` (hold ref + amount + currency), so transfer-to-destination payouts are code-complete:
- A talent registers a payout destination — `POST /v1/payments/payout-accounts` — which starts **Pending** and
  is made **Verified** via `.../{talentId}/verify` (KYC gate). We store only the provider's reference, never raw
  bank details.
- **Approve** looks up the talent's payout account and **refuses to release** unless it is Verified, then passes
  its provider reference as the transfer destination. Flutterwave does a real `/transfers`; Stripe captures the
  connected-account charge; N-Genius/DPO capture + note the separate disbursement rail (validate in sandbox).

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
