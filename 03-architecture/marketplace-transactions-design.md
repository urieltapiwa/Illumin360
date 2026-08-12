# Marketplace Transaction Layer — Design & Decision Doc

> **Status:** Proposed (no code yet) · **Owner:** Platform Architecture · **Last updated:** 2026-08-12 · **Decisions needed before implementation**

This scopes **v0.3.0 Workstream C — the transactional marketplace** (`FEATURE-PARITY.md`, GitHub issue #107). It
is a *decision document*: it lays out the approach, the fork-in-the-road choices, cost, risk, and a safe phasing
so we can commit deliberately. **No money-movement code should be written until the open decisions in §9 are
settled** — in particular the payment provider (§4) and the legal/regulatory sign-off (§7).

The product owner has decided (2026-08-12) that Illumin360 becomes **transactional**: payments/escrow,
milestones, contracts, and two-sided reviews/reputation are in scope. This doc turns that decision into a plan.

---

## 1. Why, and what "done" looks like

Today Illumin360 is a sourcing/ATS + matching platform: it connects talent (professionals, students) with
employers and moves them through a hiring pipeline, but **no value changes hands in-app**. Every commercial
freelance marketplace we benchmarked (Upwork foremost) is built on the opposite premise — the money rails *are*
the product: contracts, escrow, milestone releases, and a reputation system that the whole marketplace's trust
depends on.

**Acceptance criteria for the capability (the end state):**
- A client and a talent can agree a **contract** (fixed-price or hourly) in-app.
- Fixed-price work is split into **milestones**; the client **funds** a milestone up front (money held in
  escrow), and the talent is **paid on approval**.
- Funds are **held** between funding and release, and **released** (or **refunded**) deterministically, with a
  full **audit ledger**.
- A **dispute** can pause a release for manual resolution.
- On completion, both sides leave a **review**; reviews roll up into a **reputation score** that feeds
  search/matching (a real trust signal, à la Upwork's Job Success Score).
- **We never touch raw card/bank data** — all sensitive capture is provider-hosted (PCI SAQ-A).
- Every state transition is idempotent, webhook-driven, and reconciled against the provider.

**Explicit non-goals for the first cut:** payroll / Employer-of-Record (EOR), automated worker-classification
determinations, multi-currency FX beyond what the provider gives for free, lending/BNPL, and crypto. These are
either separate products or regulatory rabbit-holes; see §7.

---

## 2. The hard constraints that shape everything

Two constraints dominate this design and must be stated up front, because they invalidate the "obvious" answer.

### 2.1 Geography: Illumin360 is Namibia-first (NAD), not US/EU
The seed data, careers pages, and demo are Windhoek / Namibian-dollar. **This is the single biggest input to the
provider decision.** The default marketplace answer everywhere else — **Stripe Connect** — **is not available to
platforms or connected accounts in Namibia** (Stripe's supported-country list excludes NAM at time of writing).
Designing around Stripe and discovering this at build time would be a category error. The realistic providers for
NAD / Southern-African payouts are African PSPs — **Flutterwave, Paystack, DPO Group, Yoco** — whose
marketplace/split-payment and payout support differs sharply from Stripe's (see §4). If the business intends to
launch *outside* Namibia first (e.g. a US/EU pilot), Stripe Connect comes back on the table — so **"which market
first?" is a prerequisite product decision, not a technical detail.**

### 2.2 Regulatory: holding other people's money is a licensed activity
"Escrow" is not just a database column. Holding funds on behalf of two parties and releasing them on a condition
is **money transmission / a payment-institution activity** in most jurisdictions, including under the Bank of
Namibia's Payment System Management Act oversight. The **only** responsible way for a startup to offer this
without becoming a licensed payment institution is to **stand entirely on a licensed provider's regulated rails**
(the provider is the money transmitter; we are a platform orchestrating *their* holds and transfers). This is why
§4 rejects "build our own ledger + hold funds in our bank account" — that path needs a licence, a trust account,
and an auditor, and is out of scope for an engineering milestone. **Legal sign-off on the chosen model is a hard
gate (see §9).**

> **Safety note for AI-assisted development:** per the platform's own guardrails, an automated agent must never
> *execute* a fund transfer, enter financial credentials, or move money. This design keeps all actual money
> movement (a) provider-hosted and (b) triggered only by an explicit, authenticated **human** action in the UI
> (client funds a milestone; client approves a release). Our services orchestrate provider APIs and record
> ledger entries; they never hold card data and never auto-release without a human decision.

---

## 3. Recommended approach (at a glance)

**A new dedicated `Illumin360.Payments` service that orchestrates a licensed PSP's marketplace rails behind a
`IPaymentProvider` port, with a double-entry ledger for auditability — and a strict phasing that ships the
non-financial trust layer (contracts + reviews/reputation) first, money last.**

- **New service, not Billing.** `Billing` (a scaffold today) is for **our** SaaS revenue — subscriptions,
  plan pricing, invoices to employers (B2B, PCI-DSS). Marketplace escrow is a different bounded context:
  three-party (client ↔ platform ↔ talent), money *flowing through* us, KYC on talent, disputes. Mixing them
  couples two very different compliance and data models. Create `Illumin360.Payments` (own DB
  `illumin360_payments`, already provisioned by the init script? — see §9), mirroring the Candidates vertical
  slice. `Billing` stays for platform monetisation (e.g. the featured-listing / commission take-rate invoicing).
- **Provider behind a port.** Define `IPaymentProvider` in the Payments service (create connected/sub-accounts,
  create a held charge, release/transfer, refund, verify webhook signatures). Ship a deterministic
  **`FakePaymentProvider`** for tests/local (mirrors how `HashingEmbeddingProvider` lets semantic v1 run
  offline) so the whole flow is testable without a live PSP or real money.
- **Double-entry ledger.** Money as **integer minor units + ISO-4217 currency** (never floats/decimals-as-money
  in arithmetic). Every movement is two balanced ledger rows (debit/credit) against accounts
  (`client_wallet`, `escrow_hold`, `talent_wallet`, `platform_fee`), so balances are derivable and auditable and
  reconciliation against the PSP is possible. The ledger is the source of truth; the PSP is the rail.
- **Webhook-driven state machine.** Charges/transfers/payouts settle **asynchronously**; never treat an API 200
  as "money moved." Provider webhooks (signature-verified, idempotent by event id) advance the contract/milestone
  state and post ledger entries. Reuse the MassTransit outbox for our own downstream events (notify, review-unlock).
- **Ship trust before money.** Phase 0–1 (reviews/reputation, contracts, milestones as agreements) carry **no
  regulatory weight** and deliver visible marketplace value while the provider + legal decisions (§9) are
  resolved in parallel. Money (Phases 2–4) lands only after sign-off.

---

## 4. The provider decision (the central fork)

The provider choice is upstream of almost every schema and flow. Four realistic options:

| Option | Escrow / hold model | NAD / Namibia | Marketplace split & payouts | KYC / onboarding | Verdict |
|---|---|---|---|---|---|
| **Stripe Connect** | "Separate charges + transfers" or delayed payouts approximate a hold; not legal escrow | **Not supported for NAM platforms/accounts** | Best-in-class (destination charges, `transfer_data`, connected payouts) | Built-in (hosted onboarding, identity) | **Only if we launch US/EU first**; not for a Namibia launch |
| **Flutterwave** | Split payments + payout API; hold approximated by delaying disbursement to the talent subaccount | **Yes** (pan-African, NGN/ZAR/… incl. Namibia rails via partners) | Subaccounts + split at collection; payouts/transfers API | Merchant KYC; talent as subaccount/beneficiary | **Leading candidate for a Namibia/SADC launch** |
| **Paystack** (Stripe-owned, African) | Split + transfers; hold via delayed transfer | Strong ZA/NG; **verify NAD payout support** | Subaccounts + split; transfers API | Merchant KYC | Strong runner-up; check NAD payout coverage |
| **DPO Group / Yoco** | Regional PSPs; escrow-style needs manual disbursement | DPO strong in Southern Africa incl. NAM | Varies; less "marketplace-native" than Flutterwave | Varies | Fallback if Flutterwave/Paystack payout gaps |
| ~~Build our own escrow (funds in our bank)~~ | Real escrow | — | — | We become the regulated entity | **Rejected** (§2.2): needs a payment-institution licence + trust account + audit |

**Recommendation:** if launching in **Namibia/SADC**, design against **Flutterwave** (subaccounts + split +
transfers) as the reference `IPaymentProvider` implementation, and keep the port clean enough to swap. If the
business decides to **pilot in the US/EU** instead, use **Stripe Connect** — the port abstraction means the
domain/ledger/flows in §5–6 are unchanged; only the adapter differs. **This choice is DECISION D1 (§9) and blocks
Phase 2.** Every provider here approximates escrow via *held/delayed disbursement to a subaccount* rather than a
legal trust account — legal must confirm that model is acceptable for our T&Cs (§9 D2).

---

## 5. Domain model (provider-agnostic core)

All in the new Payments service; money is `long MinorUnits` + `string Currency`.

- **`Contract`** — client id, talent id, requisition/opportunity ref, type (`FixedPrice` | `Hourly`), currency,
  status (`Draft → Active → Completed | Cancelled | Disputed`), created/updated. Hourly carries a rate + a weekly
  cap; fixed-price owns milestones.
- **`Milestone`** (fixed-price) — contract id, order, title, amount (minor units), status
  (`Pending → Funded → Submitted → Approved(Released) | Refunded | Disputed`), funded/submitted/released
  timestamps. State transitions guarded exactly like our existing aggregates (409 on illegal moves).
- **`TimeEntry`** (hourly) — contract id, period, minutes, memo, status (`Logged → Approved → Invoiced`), the
  hourly analogue of a milestone for billing.
- **`LedgerAccount`** + **`LedgerEntry`** — double-entry: each movement posts balanced debit/credit rows
  referencing a `Contract`/`Milestone` and a provider reference. Account kinds: `ClientFunding`, `EscrowHold`,
  `TalentPayable`, `PlatformFee`, `Refund`. Balances are `SUM`-derived, never stored mutable.
- **`ProviderCharge`** / **`ProviderTransfer`** / **`ProviderPayout`** — thin records mapping our milestone to the
  PSP's object ids + status, with the **idempotency key** we generated and the **webhook event ids** we've
  already processed (dedupe).
- **`Payout Account`** — a talent's connected/subaccount id at the provider + KYC status (`Pending → Verified →
  Restricted`). No bank details stored by us — held at the provider.
- **`Dispute`** — milestone id, raised-by, reason, status (`Open → Resolved(Release|Refund|Split)`), notes.
- **`Review`** + **`ReputationSnapshot`** — see §8.

**Take-rate / platform fee.** The platform's commission (e.g. N%) is modelled as a `PlatformFee` ledger split at
release time (client funds 100; talent receives 100−fee; platform receives fee). Configurable, versioned per
contract so historical contracts keep their agreed rate.

---

## 6. Core flows (all human-initiated, webhook-confirmed)

Fixed-price milestone happy path:

1. **Agree** — client + talent accept a `Contract`; milestones created `Pending`. *(No money; Phase 1.)*
2. **Fund** — client clicks *Fund milestone* → we create a **provider-hosted checkout** (idempotency key =
   milestone id + attempt). Client pays on the PSP's page (we never see the card). On the `charge.succeeded`
   webhook: milestone → `Funded`, ledger posts `ClientFunding → EscrowHold`.
3. **Work & submit** — talent submits deliverables; milestone → `Submitted`.
4. **Approve → release** — client clicks *Approve* → we call the provider **transfer/disbursement** to the
   talent's subaccount minus the platform fee. On the `transfer.succeeded` / `payout.*` webhook: milestone →
   `Approved(Released)`, ledger posts `EscrowHold → TalentPayable` + `EscrowHold → PlatformFee`.
5. **Complete** — when all milestones released, `Contract → Completed`; **review unlocked** for both sides (§8).

Branches: **auto-approve timer** (optional, configurable "approve within N days or auto-release" — still a
pre-agreed human rule, not the agent acting); **refund** (client+talent agree, or dispute resolves that way) →
provider refund → ledger `EscrowHold → Refund`; **dispute** (either party) → milestone `Disputed`, release
blocked until an admin resolves to release/refund/split.

Every step is **idempotent** (safe to retry; provider idempotency keys + our processed-webhook-id set) and
**reconciled** (a periodic job compares ledger balances to provider balances and flags drift — like the existing
`JobAlertScheduler`/`NurtureScheduler` background pattern).

---

## 7. Trust, safety, compliance

- **PCI:** SAQ-A only — all card capture is provider-hosted (redirect / hosted fields). Our services never
  receive, log, or store PAN/CVV. This aligns with the platform rule that card data is never entered into our
  own fields.
- **KYC/AML:** the talent must complete the provider's onboarding (identity, payout account) before they can be
  paid; `PayoutAccount.Status` gates release. AML/screening is the provider's regulated responsibility.
- **Worker classification / EOR:** **out of scope** (a non-goal). We are a marketplace connecting independent
  parties, not an employer. Contracts carry clear independent-contractor T&Cs; we do **not** make
  classification determinations or run payroll. Note this explicitly in T&Cs; revisit only as a separate product.
- **Tax:** the provider issues the tax artefacts it's obliged to (e.g. payout reporting); we surface earnings
  summaries but do **not** file taxes for users in v1.
- **Disputes/chargebacks:** modelled (`Dispute`) with an admin resolution surface; chargeback webhooks reverse
  ledger entries and flag the contract.
- **Legal gate:** T&Cs, the escrow/hold model, the money-transmission posture, and consumer-protection
  obligations need **counsel sign-off before Phase 2** (D2 in §9). This is not optional.

---

## 8. Reviews & reputation (ships first — Phase 0)

Deliberately the **first** thing we build, because it's high-value, **carries no regulatory weight**, and seeds
the trust system the money layer later leans on.

- **`Review`** — two-sided, unlocked when a contract completes (or a milestone releases): rating (1–5) +
  comment, from-role (client/talent), visible after both submit or a window closes (double-blind, like Ashby's
  feedback-blinding and Upwork's mutual-visibility rule) to reduce retaliation bias.
- **`ReputationSnapshot`** — a rolled-up score per talent (and per client): blends rating average, completion
  rate, on-time rate, and recency — an Upwork-JSS-style signal. Pure, deterministic, unit-testable (same shape
  as `RediscoveryScorer` / `MatchScorer`), living in `Illumin360.Matching` so it can **feed ranking**: a
  reputation term in `MatchExplanation` and a tie-breaker in the learned ranker's feature vector later.
- Endpoints + portal surfaces mirror the existing offer/onboarding panels.

**Phase 0 needs no provider and no legal gate** — it can start immediately and is the recommended first PR of
Workstream C.

---

## 9. Open decisions (must be settled before the gated phases)

| # | Decision | Blocks | Recommendation |
|---|---|---|---|
| **D1** | **Launch market** (Namibia/SADC vs US/EU pilot) → **payment provider** | Phase 2+ | **REOPENED (2026-08-12) after verifying the real docs — see §12.** No candidate offers a documented **NAD/Namibia payout** rail. All four adapters exist behind the port (default **Fake**/off) and the port now carries destination+amount, but the *provider* choice for a Namibia-first marketplace is unresolved: it needs a corridor/rail confirmed with a provider, not a code choice. |
| **D2** | **Legal/regulatory sign-off** on the escrow-via-provider model, T&Cs, money-transmission posture | Phase 2+ | Engage counsel now, in parallel with Phase 0–1. Hard gate. |
| **D3** | **Platform take-rate** model + value (flat %? tiered? who pays — client, talent, or split?) | Phase 3 (release splits) | Model as a versioned `PlatformFee` ledger split; value is a business call. |
| **D4** | **`illumin360_payments` database** provisioning (the init script lists `billing` but not `payments`) | Phase 1 | Add `payments` to `deploy/docker/init/01-create-databases.sh`; or, if we instead extend `Billing`, revisit §3. |
| **D5** | **Hourly vs fixed-price for v1** — do we need both, or is fixed-price + milestones enough to launch? | Phase 1 scope | Recommend **fixed-price + milestones first**; hourly (time logs + auto-invoicing) as a fast-follow. |
| **D6** | **Dispute resolution** — in-house admin only, or a provider/third-party arbitration path? | Phase 4 | In-house admin resolution for v1 (release/refund/split); revisit at scale. |

---

## 10. Phasing (each phase is shippable; money is last)

- **Phase 0 — Reviews & reputation** *(no provider, no legal gate).* ✅ **Shipped (2026-08-12).** Realized in
  Recruitment against the engagement primitive that exists today — a **hired application** — rather than a
  contract (contracts arrive in Phase 1). `EngagementReview` (double-blind: both sides submit before either is
  visible) + pure `ReputationScorer` in `Illumin360.Matching` (Bayesian-shrunk 0–100). Endpoints
  `POST/GET /applications/{id}/review[s]` + `GET /talents/{id}/reputation`; talent- and employer-side review
  surfaces. When the Payments service lands, reviews extend to contract completion behind the same scorer.
- **Phase 1 — Contracts & milestones as agreements** *(no money).* ✅ **Shipped (2026-08-12).** New
  `Illumin360.Payments` service (own DB `illumin360_payments`, gateway `/api/payments`, port 5207):
  `Contract` + `Milestone` state machines, append-only `LedgerMovement`, `IPaymentProvider` port +
  **`FakePaymentProvider`** (no real money), full fund → submit → approve/refund flow, contract auto-completes
  when all milestones settle. Unit + Testcontainers integration tests (full lifecycle over HTTP). *(D4 defaulted
  — `payments` added to the init script; D5 defaulted — fixed-price + milestones first, hourly deferred. Portal
  UI + real PSP adapter are the follow-ups.)*
- **Phase 2 — Fund escrow** *(gated: D1, D2).* `IPaymentProvider` real adapter, provider-hosted funding,
  `charge.*` webhooks, ledger `ClientFunding → EscrowHold`. Money can go *in*.
- **Phase 3 — Release / refund / payouts** *(gated: D1, D2, D3).* Transfers/disbursements to talent subaccounts,
  platform-fee split, refunds, reconciliation job. Money can come *out*.
- **Phase 4 — Disputes & polish** *(gated: D6).* Dispute lifecycle + admin resolution, chargeback handling,
  earnings summaries.

**Immediate next step:** if approved, open a Phase-0 PR (reviews & reputation) — it needs none of the open
decisions and starts delivering the marketplace-trust layer now, while D1/D2 are worked in parallel.

---

## 11. What this is *not*

A licence to move money in code. Nothing in Phases 2–4 is built until D1 (provider) and D2 (legal) are signed
off. Phases 0–1 are pure product/engineering and safe to start. The ledger + port design means the provider can
be chosen — or changed — late without reworking the domain.

---

## 12. Provider verification findings (2026-08-12) — read this before Phase 2

The four adapters were re-checked against each provider's **live developer documentation** (not memory). Two
things changed materially.

### 12.1 Payout capability is split — only two can pay a third party
The marketplace's core money move is **paying the talent** (release escrow to a seller). Verified:

| Provider | Collect / hold | Refund to payer | **Pay out to talent (third party)** | NAD / Namibia |
|---|---|---|---|---|
| **Flutterwave** v3 | ✅ `/payments` (hosted link; async verify) | ✅ `/transactions/{id}/refund` | ✅ **Subaccounts (split) / `/transfers`** | ❌ **not a Flutterwave market/currency** |
| **Stripe** | ✅ manual-capture PaymentIntent | ✅ `/refunds` (+ reverse_transfer) | ✅ **Connect transfers / destination charges** | ❌ **Stripe unavailable for Namibia** |
| **N-Genius** (Network Intl) | ✅ order `AUTH` | ✅ (to payer, HATEOAS-scoped) | ❌ **no third-party payout API** (card acquiring) | MEA-focused |
| **DPO Group** | ✅ `createToken` (payment request) | ✅ `refundToken` | ❌ **no third-party payout API** (collection-only) | ✅ **operates in NAM — for collection** |

So **N-Genius and DPO cannot pay the talent at all** via their public APIs — their adapters now return an
explicit not-supported result on `Release` (settle-to-merchant + disburse out-of-band). Only **Flutterwave** and
**Stripe** have a real payout primitive.

### 12.2 The blocker: no verified NAD payout rail
- **Flutterwave** — Namibia/NAD is **not** in its settlement/payout markets (NGN, GHS, KES, ZAR, UGX, TZS, RWF,
  ZMW, XAF, XOF, EGP, MWK; card acquiring in UK/US/EU). NAD collection/payout is **not documented as supported**.
- **Stripe** — **not available** for platforms or connected accounts based in Namibia (supported-countries list).
- **DPO** — *collects* in Namibia (NAD acquiring), but has **no payout API** to pay the talent.
- **N-Genius** — MEA card acquiring; no payout.

**Net: a Namibia-first marketplace that pays talent in NAD cannot be built on any of these four's documented
payout APIs today.** This is a go/no-go input for the transactional-marketplace direction — it is not something
more code fixes.

### 12.3 Recommended next steps (business, then build)
1. **Confirm corridors with sales, not docs** — ask Flutterwave and DPO directly whether NAD collection +
   (crucially) **payout/settlement to Namibian talent** is available under a commercial/settlement arrangement
   (these often exist off the public API).
2. **If payout stays unavailable via PSP:** collect via **DPO** (NAD acquiring) into the platform balance, and
   disburse to talent via a **separate rail** — Namibian **bank EFT** and/or **mobile money** — with our ledger
   as the source of truth. That means adding a *disbursement* port distinct from the *acquiring* port.
3. **If the beachhead can be non-Namibian:** Stripe Connect (US/EU) or Flutterwave (NGN/KES/ZAR/GHS…) both work
   end-to-end today — a market-selection decision, not a technical one.
4. Until one of the above is confirmed, **keep `Payments:Provider=Fake`** — the domain, ledger, contracts,
   milestones, payout-account KYC gate, and reviews all work; only the real money leg is blocked.

The adapter code is faithful to each provider's documented API for what it *can* do; the collection-only
providers are labelled and fail loudly on payout rather than pretending. Nothing here is a substitute for a
sandbox integration + the D2 legal sign-off.
