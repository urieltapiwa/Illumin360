# Illumin360 — Talent Match & Recruitment Platform

> Illumin Investments CC (CC 2016/08234 | VAT 07851437-015) — Trading as **Illumin** | Product: **Illumin360**
> www.illumininvestments.com · projects@illumininvestments.com

Illumin360 is an AI-assisted talent-matching and recruitment platform connecting **Professionals**,
**Students**, and **Businesses**, with internal **Support** and **Administrator** operations. This
repository holds the **engineering codebase and SDLC-as-code artefacts**. The detailed business and
design documentation lives in the numbered `01_Project_Initiation` … `14_Configuration_Asset_Management`
folders (the canonical SDLC document set) and the `_v2.0_refresh/` corrections.

This engineering scaffold was generated from the **AI Project Bootstrap Charter** (see `CHARTER.md`).

---

## What's here

| Path | Purpose |
| --- | --- |
| `CHARTER.md` | The completed engineering charter (binding standards for the whole project). |
| `00-governance/` | ADRs, RFCs, policies, OSS licence inventory + SBOMs. |
| `01-discovery/` · `02-requirements/` · `04-design/` | As-code pointers into the canonical `0x_*` document folders. |
| `03-architecture/` | C4 diagrams (Mermaid), solution/data/security/observability architecture, API standards. |
| `05-quality/` | Test strategy, plans, performance baselines, security-testing cadence. |
| `06-operations/` | Runbooks, SLO/SLI, incident response, Grafana dashboards & alerts (as code). |
| `07-release/` | Release plan, environment parity, rollback strategy. |
| `deploy/` | Docker Compose (local), Helm/k8s (deploy), Terraform (IaC), observability + Keycloak config. |
| `src/` | .NET 10 solution: BuildingBlocks, YARP Gateway, microservices, BFFs, MVC apps. |
| `.github/` | CI/CD workflows, issue & PR templates. |

## Technology stack (pinned — all open source)

- **Runtime:** .NET 10 (LTS), C# 14 · .NET Aspire 13 (dev inner loop)
- **Services:** ASP.NET Core Minimal APIs · gRPC internal · REST external
- **Frontends:** ASP.NET Core MVC (5 portals) each behind its own **BFF**
- **Edge:** YARP reverse proxy
- **IAM:** Keycloak 26.6.x (OIDC / OAuth 2.1)
- **Data:** PostgreSQL 17 (DB-per-service) · EF Core 10 · Redis · **MinIO** (object storage)
- **Messaging:** RabbitMQ + MassTransit (outbox, sagas) · **Temporal** (long-running workflows)
- **Resilience:** Polly (Microsoft.Extensions.Http.Resilience)
- **Observability:** OpenTelemetry → Grafana **Alloy** → **LGTM** (Loki, Grafana, Tempo, Mimir) + Prometheus · Serilog
- **Security tooling:** ClamAV (file scanning) · HashiCorp Vault (secrets) · Trivy · OWASP ZAP · gitleaks · CycloneDX SBOM
- **Containers:** Docker + Compose (local) · Kubernetes + Helm (deploy)

## Quick start (local environment)

Prerequisites: **Docker Desktop** (or Docker Engine + Compose v2) and the **.NET 10 SDK** (for `src/`).

```bash
# 1. Copy environment defaults and review them
cp deploy/docker/.env.example deploy/docker/.env

# 2. Bring up the platform infrastructure (Postgres, Redis, Keycloak, RabbitMQ, MinIO)
docker compose -f deploy/docker/docker-compose.yml up -d

# 3. Bring up the observability stack (Alloy, Prometheus, Loki, Tempo, Mimir, Grafana)
docker compose -f deploy/docker/docker-compose.yml -f deploy/observability/docker-compose.observability.yml up -d

# 4. Check service health
docker compose -f deploy/docker/docker-compose.yml ps
```

| Service | URL (local) | Default credentials |
| --- | --- | --- |
| Keycloak | http://localhost:8080 | `admin` / `admin` (dev only) |
| Grafana | http://localhost:3000 | `admin` / `admin` (dev only) |
| RabbitMQ mgmt | http://localhost:15672 | `illumin` / `illumin` (dev only) |
| MinIO console | http://localhost:9001 | `illumin` / `illumin12345` (dev only) |
| Postgres | localhost:5432 | per `.env` |

> ⚠️ All credentials above are **development defaults**. Never use them outside local. Production secrets are
> managed in **HashiCorp Vault** — see `00-governance/policies/security-policy.md`.

## Building the .NET solution

```bash
cd src
dotnet restore
dotnet build -warnaserror
dotnet test
```

The **Candidates** service is implemented as a complete, fully-instrumented **vertical slice**
(one endpoint end-to-end through Domain → Application → Infrastructure → Api, with health checks,
OpenTelemetry, OpenAPI, and tests) per the charter's working method. Remaining services follow the
identical Clean Architecture layout.

## Contributing

See `CONTRIBUTING.md`, `00-governance/policies/`, and the **Definition of Done** in `CHARTER.md` (Part 19).
