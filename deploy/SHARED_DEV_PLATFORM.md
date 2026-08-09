# Illumin360 — shared dev platform (Keycloak + Grafana moved out)

**What changed (local dev only):** Illumin360 no longer runs its own Keycloak or
Grafana. They now live in a shared stack used by every app on this machine, to
save memory/disk. Everything else (Postgres, Redis, RabbitMQ, MinIO, Alloy,
Prometheus, Mimir, Loki, Tempo, the .NET services) still runs here, unchanged.

Shared platform location: **`../../dev-platform`**
(`C:\Users\Dsamu\Downloads\dev-platform`).

| Resource | Before | Now |
|----------|--------|-----|
| Keycloak | `illumin360-keycloak` on :8080 | shared `devplatform-keycloak` on **:8080**, realm **`illumin360`** |
| Grafana  | `illumin360-grafana` on :3000  | shared `devplatform-grafana` on **:3000**, org **`Illumin360`** |
| LGTM backends | own + host ports | own (unchanged), **host ports removed** — shared Grafana reaches them in-network as `illumin360-mimir/-prometheus/-loki/-tempo` |

No app config changed: services still use `http://keycloak:8080/realms/illumin360`
— the shared Keycloak joins this stack's `illumin360` network and resolves as
the same `keycloak` hostname.

## Where to continue / how to run

```powershell
# 1. ONE TIME on this machine — create the shared Docker networks
cd C:\Users\Dsamu\Downloads\dev-platform
./setup.ps1

# 2. Start the shared platform (Keycloak + Grafana). Leave it running.
docker compose -f docker-compose.platform.yml up -d

# 3. Start Illumin360 as before
cd C:\Users\Dsamu\Downloads\Illumin360\Illumin360
docker compose -f deploy/docker/docker-compose.yml `
               -f deploy/observability/docker-compose.observability.yml `
               -f deploy/docker/docker-compose.apps.yml up -d --build
```

The networks are external/pre-created, so step order is flexible. Keycloak's
**first** boot takes ~2 min (DB schema init) — not a hang.

## Endpoints

- Keycloak admin: http://localhost:8080  (admin / admin)
- Grafana: http://localhost:3000  (admin / admin) → org **Illumin360** (default).
  Illumin360 dashboards from `06-operations/dashboards` are mounted there.
- Dev users in realm `illumin360`: `dev.professional` and `dev.admin`, password `Password123!`.

## Notes

- To re-expose a backend UI directly (e.g. Prometheus on the host), add a
  `ports:` entry back to that service in
  `deploy/observability/docker-compose.observability.yml`.
- Full details, realm exports and the Grafana org bootstrap live in
  `../../dev-platform/README.md`.
