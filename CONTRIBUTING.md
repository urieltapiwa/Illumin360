# Contributing to Illumin360

## Ground rules
1. **Open source only.** Every new dependency must carry a permissive/compatible licence (MIT, Apache-2.0,
   BSD, MPL-2.0). Record it in `00-governance/licenses/` and confirm in an ADR if non-obvious.
2. **Clean Architecture.** Source dependencies point inward only (Domain → Application → Infrastructure → Presentation).
3. **Document as you build — both doc sets stay current.** XML doc comments, ADRs, READMEs, and runbooks are
   part of "done" (see `CHARTER.md` Part 19). Any change that alters behaviour, architecture, schema, public
   API, deployment, or dependencies MUST update the affected **living markdown docs** (`00-governance/adr`,
   `03-architecture/*`, `06-operations/runbooks/*`, `CHANGELOG.md`) **and** the corresponding **formal
   deliverables** (`01_…`–`14_…` / `_v2.0_refresh`) in the same change. New decision → new ADR; new event →
   update `03-architecture/integration-architecture.md`'s event catalogue; new service → add a runbook.
4. **Secure by default.** No secrets in source. Threat-model before building.

## Branching & commits
- **Trunk-based** development with short-lived feature branches.
- **Conventional Commits** (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:` …) so changelogs and
  SemVer versioning can be automated.
- PRs require: green CI, a CODEOWNERS review, and a satisfied Definition of Done checklist.

## Local workflow
```bash
docker compose -f deploy/docker/docker-compose.yml up -d   # infra
cd src && dotnet build -warnaserror && dotnet test          # build + test
```

## Adding a new microservice
Copy the structure of `src/Services/Candidates/` (the reference vertical slice):
`*.Domain` → `*.Application` → `*.Infrastructure` → `*.Api` + `tests/` (Unit, Integration, Contract).
Each service: its own Postgres database, OpenAPI spec in `04-design/api-contracts/`, Dockerfile,
`/health/{live,ready,startup}` probes, OTel wiring via `AddProjectObservability()`, a Grafana dashboard
in `06-operations/dashboards/`, and a runbook in `06-operations/runbooks/`.

## Definition of Ready / Done
See `05_Agile_Sprint_Management/` (canonical docs) and `CHARTER.md` Part 19.
