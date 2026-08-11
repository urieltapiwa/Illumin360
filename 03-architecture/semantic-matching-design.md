# Semantic (Embedding) Matching — Design & Decision Doc

> **Status:** Proposed (no code yet) · **Owner:** Platform Architecture · **Last updated:** 2026-08-11 · **Decision needed before implementation**

This scopes **Tier 2 — semantic/embedding matching** from the deep parity audit (`FEATURE-PARITY.md`). It is a
*decision document*: it lays out the approach, options, cost and risks so we can choose deliberately. **No code
should be written until the open decisions below are settled.**

---

## 1. Why, and what "done" looks like

Today `Illumin360.Matching.MatchScorer` is a pure, deterministic **keyword/token** scorer (city + role-title
overlap + skill-token containment, plus optional salary/seniority). It cannot see that "K8s" ≈ "Kubernetes",
"RN" ≈ "React Native", or that "built payment rails" is relevant to a "Payments Engineer" role. Semantic
matching closes that gap by comparing **meaning**, not tokens.

**Acceptance criteria for the feature:**
- A candidate/role can be encoded into a vector and stored.
- Given a role (or a candidate), return the top-N semantically closest candidates (or roles) ranked by
  cosine similarity.
- Semantic score is **blended with** — not a replacement for — the existing signals, and stays explainable
  (feeds a new "Semantic fit" line in the existing `MatchExplanation`).
- Deterministic, testable seams (embedding calls mockable; ranking math pure).

**Explicit non-goals for the first cut:** learning-to-rank / feedback loops (separate Tier 2 item), cross-service
real-time re-embedding on every keystroke, and multi-lingual tuning beyond what the chosen model gives for free.

---

## 2. Recommended approach (at a glance)

**pgvector on the existing per-service PostgreSQL + a pluggable embedding provider behind an interface, with a
deterministic hashing provider as the default/offline implementation.**

- **Store:** add the `vector` column type via the **pgvector** extension to the service that owns the data
  (Candidates for candidate vectors; Recruitment for role vectors). No new datastore — fits
  database-per-service (data-architecture.md).
- **Embed:** define `IEmbeddingProvider` in `Illumin360.Matching` (or a new `Illumin360.Matching.Embeddings`
  building block). Ship a **deterministic, dependency-free `HashingEmbeddingProvider`** as the default so the
  feature works in dev/CI/offline with zero external calls; allow a **hosted-API provider** (OpenAI/Cohere/…)
  to be swapped in by configuration for production quality.
- **Rank:** cosine similarity via pgvector's `<=>` operator (ANN index), returning candidate ids + distances;
  blend the normalised similarity into `MatchScorer` as one more weighted signal.

Rationale: reuses Postgres (no Redis/Elastic/dedicated vector DB to run), keeps the "pure + testable" ethos via
the hashing default, and makes the expensive/external part (a real embedding model) an opt-in config choice
rather than a hard dependency.

---

## 3. Options considered

### 3a. Vector store
| Option | Pros | Cons | Verdict |
| --- | --- | --- | --- |
| **pgvector (in each service DB)** | No new infra; per-service ownership preserved; SQL-native ANN (HNSW/IVFFlat); EF-mappable | Needs the extension enabled in the image/init; ANN index tuning | **Recommended** |
| Dedicated vector DB (Qdrant/Weaviate/Pinecone) | Best-in-class ANN, filtering | New service to run/secure/back-up; another data home outside the DB-per-service model; Pinecone = SaaS + cost | Rejected for v1 |
| In-memory (FAISS-in-process / brute force) | Trivial for small pools | State lost on restart; doesn't scale; re-embed on boot | Rejected (except as the math path behind small pools) |

### 3b. Embedding provider
| Option | Pros | Cons | Verdict |
| --- | --- | --- | --- |
| **Hashing/bag-of-words vector (local, deterministic)** | Zero deps, offline, free, CI-friendly, deterministic tests | Not true semantics — captures term overlap, some synonym-ing only if we add a synonym map | **Default** (dev/CI + graceful prod fallback) |
| Hosted API (OpenAI `text-embedding-3-small`, Cohere embed) | Genuine semantics, multilingual, cheap per call | External dependency + network + API key/secret mgmt + per-call cost + PII leaving the boundary | **Opt-in prod** (behind config) |
| Self-hosted model (e.g. bge-small via ONNX in-process) | Real semantics, no data egress, no per-call cost | Ships a model file (~100MB) + ONNX runtime; CPU cost; more moving parts | Future option; revisit if data-egress is a blocker |

### 3c. When to embed
- **On write** (candidate registered / CV parsed / role posted / enrichment edited) — embed and store the vector
  in the same transaction path (or via the outbox for the API-provider case, since it's an external call).
- **Backfill** — a one-off command to embed existing rows after rollout.
- Avoid embedding on read/query (latency + cost).

---

## 4. What text gets embedded

- **Candidate vector:** headline + skills (Professionals) / headline + city + parsed-CV skills (Candidates).
  Candidates currently carry no structured skills, so the CV text (already extractable via `Illumin360.Resume`)
  is the richest source — embed the parsed CV text when present, else the headline.
- **Role vector:** title + industry + requisition tags + salary/seniority context.
- **PII note:** embedding CV text sends candidate data to the provider when the hosted API is selected. For the
  hosted path we must (a) strip direct identifiers before embedding (reuse `BlindRedactor` thinking), and (b)
  get this past the security-architecture data-egress rules. The **hashing default keeps everything in-process**,
  so v1 can ship with no egress and the API path can be enabled later under governance.

---

## 5. Proposed shape (illustrative — not final)

```
Illumin360.Matching (or .Embeddings)
  IEmbeddingProvider           // Task<float[]> EmbedAsync(string text, CancellationToken)
  HashingEmbeddingProvider     // deterministic, dependency-free default (fixed dim, e.g. 256)
  VectorMath                   // pure: cosine similarity, normalise (unit-tested)

Candidates / Recruitment (owning services)
  candidate_embeddings(request/candidate_id, embedding vector(N), model, updated_at)  // pgvector
  GET /v1/candidates/{id}/semantic-similar        // ANN over candidate_embeddings
  (recruitment) role vectors + "semantic top candidates"
```

- `MatchScorer` gains an optional pre-computed `SemanticSimilarity` input (0–1) that becomes a weighted signal
  in `Explain`/`Score` (renormalised, exactly like salary/seniority — so callers without it are unchanged).
- The ANN query lives in the repository (it's SQL); the blend + explanation stays in the pure engine.

---

## 6. Migration & ops impact

- **DB image:** enable `CREATE EXTENSION vector;` in the Postgres init for the Candidates (and Recruitment) DBs.
  Confirm the base Postgres image ships pgvector or switch to `pgvector/pgvector:pg17`.
- **EF Core:** `Npgsql.EntityFrameworkCore.PostgreSQL` supports `vector` mapping via the pgvector plugin
  (`Pgvector` + `UseVector()`); add the package (Central Package Management) — **one new dependency**.
- **Migrations:** one per owning service adding the embeddings table + HNSW index.
- **Backfill job:** a one-shot to embed existing candidates/roles.
- **Config/secrets:** if/when the hosted provider is enabled — API key via the existing secret mechanism; a
  circuit-breaker/fallback to the hashing provider on outage so matching never hard-fails.

---

## 7. Risks

- **Data egress / PII** (hosted API) — the main governance blocker; mitigated by the in-process hashing default
  and identifier-stripping before any external embed.
- **Cost drift** (hosted API) — bounded by embed-on-write + backfill (not per-query) and small models.
- **Index tuning** — HNSW parameters affect recall/latency; start with defaults, measure on real pool size.
- **Quality of the hashing default** — it is *not* true semantics; be honest in UI copy ("keyword + vector
  fit") until a real model is enabled. It mainly de-risks delivery and keeps CI hermetic.
- **Scope creep into LTR** — keep feedback-loop learning out of this piece.

---

## 8. Open decisions (needed before coding)

1. **Provider for production:** ship hashing-only for v1, or wire a hosted API (which one) now? → drives the
   PII/egress and secrets work.
2. **Data-egress approval:** is sending (redacted) CV text to an external embedding API acceptable under
   security-architecture? If not, hashing-only or self-hosted model only.
3. **Scope of v1:** candidate-side "similar/semantic" only, or both candidate↔role directions?
4. **Where role vectors live:** Recruitment (owns requisitions) — confirm it takes the pgvector dependency too.
5. **Rollout:** backfill strategy + whether semantic is on by default or behind a flag.

---

## 9. Recommendation

Ship **v1 = pgvector + `IEmbeddingProvider` with the deterministic `HashingEmbeddingProvider` default**, blended
into the existing explainable engine, candidate-side first. This delivers the *plumbing, storage, ranking and
explainability* with **no external dependency, no data egress, and hermetic tests** — then enabling a real
hosted (or self-hosted) embedding model becomes a **config change**, gated on decisions (1)–(2), not a rebuild.

If we instead want true semantics from day one, the smallest step up is `text-embedding-3-small` behind the same
interface, pending data-egress sign-off.
