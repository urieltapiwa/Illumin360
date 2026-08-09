# Keycloak — Illumin360 realm & login theme

| Path | Purpose |
| --- | --- |
| `illumin360-realm.json` | Realm definition (imported on Keycloak startup). Sets `loginTheme: illumin360`, password policy, roles, clients. |
| `themes/illumin360/` | Branded **login theme** (dark green, matching the portals). |

## How it's wired today (local dev)
The IAM is the **shared dev-platform Keycloak** (`devplatform-keycloak`, in `~/Downloads/dev-platform`), used by Illumin360, SalesApp and StoreCatalogue. It:
- imports realms from a bind mount → `dev-platform/keycloak/realms/` ⇒ `/opt/keycloak/data/import`
- runs `start-dev` (theme caching **off**, so theme CSS edits apply live)

The theme was applied at runtime (`docker cp` of `themes/illumin360` into the container + realm `loginTheme` set via the Admin API). That survives `docker restart` but **not** a container recreate (`up --build` / `rm`), because the theme files aren't mounted.

## Make it persistent (survives container recreate)
Because this Keycloak is **shared**, do this deliberately — a recreate re-imports every app's realm.

1. **Mount the theme** — add to the `keycloak` service in `dev-platform/docker-compose.platform.yml`:
   ```yaml
   volumes:
     - ../Illumin360/Illumin360/deploy/keycloak/themes:/opt/keycloak/themes:ro   # adjust relative path
   ```
2. **Ship the realm with the theme set** — copy this realm file into the import dir:
   ```bash
   cp deploy/keycloak/illumin360-realm.json ~/Downloads/dev-platform/keycloak/realms/
   ```
   (It now contains `"loginTheme": "illumin360"`.)
3. **Apply**:
   ```bash
   docker compose -f ~/Downloads/dev-platform/docker-compose.platform.yml up -d keycloak
   ```

## Notes
- The SPA client `illumin360-business` and the demo user (`demo` / `Illumin360Demo!`) were created via the Admin API. To persist them too, add them to the `clients` / `users` arrays of `illumin360-realm.json` before re-import.
- Keycloak **error** pages (e.g. an expired auth session) use the default theme, not `loginTheme` — that's expected and not a theme bug.
