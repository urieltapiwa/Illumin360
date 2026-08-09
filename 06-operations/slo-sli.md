# SLOs / SLIs & Error Budgets

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-05-29 · **Related ADRs:** —

| Service | SLI | SLO | Error budget |
| --- | --- | --- | --- |
| Gateway | Availability | 99.9% / 30d | 43m 49s |
| Candidates | p95 latency (read) | < 300 ms | — |
| Candidates | Success rate | ≥ 99.9% | 0.1% |
| Billing | Payment webhook success | ≥ 99.95% | 0.05% |

Alert on symptoms + multi-window multi-burn-rate budget burn (charter Part 10.5). Every alert links a runbook.
