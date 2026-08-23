#!/usr/bin/env bash
# Provision the six per-portal Keycloak realms (admin/student/professional/business/employer/support)
# from the exports in ./realms, then create the self-registration service client in the four
# self-registration realms. Idempotent: existing realms are left in place (pass --recreate to replace).
#
# Uses the master-realm `claude-automation` service account (client_credentials) via the admin REST API,
# so no Keycloak restart is required. The realm JSONs are also mounted by the dev-platform Keycloak
# (--import-realm) so a cold Keycloak re-imports them.
#
#   KC=http://localhost:8080 CLAUDE_AUTOMATION_SECRET=... ./provision-realms.sh [--recreate]
set -euo pipefail

KC="${KC:-http://localhost:8080}"
SECRET="${CLAUDE_AUTOMATION_SECRET:-AxYv56zSD30seeMWLnzPvuS9bKQppAao}"
REG_SECRET="${REGISTRATION_CLIENT_SECRET:-CHANGE_ME_registration_dev_secret}"
RECREATE="${1:-}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# realm -> realm roles the registration service account holds. It must hold every role it assigns to
# newly-registered users, plus (student/professional) admin.write so its client_credentials service token
# is accepted by the students/professionals services when creating the domain profile. business/employer
# are identity-only (no domain profile), so they need no admin.write.
declare -A REG_ROLES=(
  [student]="client.user client.student admin.write"
  [professional]="client.user admin.write"
  [business]="client.business"
  [employer]="client.employer client.user"
)
REALMS=(admin student professional business employer support)

echo "==> obtaining master admin token (claude-automation)"
TOK=$(curl -s --fail "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=client_credentials -d client_id=claude-automation -d client_secret="$SECRET" \
  | python -c "import sys,json;print(json.load(sys.stdin)['access_token'])")
auth=(-H "Authorization: Bearer $TOK")

realm_exists() { [ "$(curl -s -o /dev/null -w '%{http_code}' "${auth[@]}" "$KC/admin/realms/$1")" = "200" ]; }

for realm in "${REALMS[@]}"; do
  file="$HERE/realms/$realm-realm.json"
  echo "==> realm '$realm'"
  if realm_exists "$realm"; then
    if [ "$RECREATE" = "--recreate" ]; then
      echo "    exists -> deleting (--recreate)"
      curl -s -X DELETE "${auth[@]}" "$KC/admin/realms/$realm" >/dev/null
    else
      echo "    exists -> skipping realm import (use --recreate to replace)"
    fi
  fi
  if ! realm_exists "$realm"; then
    code=$(curl -s -o /dev/null -w '%{http_code}' -X POST "${auth[@]}" \
      -H "Content-Type: application/json" --data-binary @"$file" "$KC/admin/realms")
    echo "    import -> HTTP $code"
    [ "$code" = "201" ] || { echo "    !! import failed"; exit 1; }
    sleep 2 # let Keycloak finish creating the realm's default clients (realm-management, etc.)
  fi

  # Self-registration service client (only for the four self-registration realms).
  roles="${REG_ROLES[$realm]:-}"
  [ -z "$roles" ] && continue
  echo "    configuring illumin360-registration service client (roles: $roles)"

  reg_cid() { curl -s "${auth[@]}" "$KC/admin/realms/$realm/clients?clientId=illumin360-registration" \
    | python -c "import sys,json;d=json.load(sys.stdin);print(d[0]['id'] if isinstance(d,list) and d else '')"; }
  cid=$(reg_cid)
  if [ -z "$cid" ]; then
    curl -s -X POST "${auth[@]}" -H "Content-Type: application/json" \
      -d "{\"clientId\":\"illumin360-registration\",\"enabled\":true,\"protocol\":\"openid-connect\",\"publicClient\":false,\"secret\":\"$REG_SECRET\",\"serviceAccountsEnabled\":true,\"standardFlowEnabled\":false,\"directAccessGrantsEnabled\":false}" \
      "$KC/admin/realms/$realm/clients" >/dev/null
    for _ in 1 2 3 4 5; do cid=$(reg_cid); [ -n "$cid" ] && break; sleep 1; done
    [ -z "$cid" ] && { echo "    !! could not create registration client"; exit 1; }
  fi

  said=""
  for _ in 1 2 3 4 5; do
    said=$(curl -s "${auth[@]}" "$KC/admin/realms/$realm/clients/$cid/service-account-user" \
      | python -c "import sys,json;d=json.load(sys.stdin);print(d.get('id','') if isinstance(d,dict) else '')")
    [ -n "$said" ] && break; sleep 1
  done
  [ -z "$said" ] && { echo "    !! no service-account user"; exit 1; }

  # realm-management client roles: manage-users + view-users (create users, send verify email, query).
  rmid=""
  for _ in 1 2 3 4 5; do
    rmid=$(curl -s "${auth[@]}" "$KC/admin/realms/$realm/clients?clientId=realm-management" \
      | python -c "import sys,json;d=json.load(sys.stdin);print(d[0]['id'] if isinstance(d,list) and d else '')")
    [ -n "$rmid" ] && break; sleep 1
  done
  [ -z "$rmid" ] && { echo "    !! no realm-management client"; exit 1; }
  # manage-users to create users + assign role mappings; view-realm to read the realm role definitions
  # the registrar looks up before assigning them.
  rm_json=$(curl -s "${auth[@]}" "$KC/admin/realms/$realm/clients/$rmid/roles")
  mapping=$(echo "$rm_json" | python -c "import sys,json;w={'manage-users','view-users','query-users','view-realm'};print(json.dumps([r for r in json.load(sys.stdin) if r['name'] in w]))")
  curl -s -X POST "${auth[@]}" -H "Content-Type: application/json" -d "$mapping" \
    "$KC/admin/realms/$realm/users/$said/role-mappings/clients/$rmid" >/dev/null
  echo "      + realm-management: manage-users, view-users, query-users, view-realm"

  # Realm roles the registrar assigns to newly-created users (the SA must hold them to grant them).
  for role in $roles; do
    rr=$(curl -s "${auth[@]}" "$KC/admin/realms/$realm/roles/$role" \
      | python -c "import sys,json;d=json.load(sys.stdin);print(json.dumps([{'id':d['id'],'name':d['name']}]))")
    curl -s -X POST "${auth[@]}" -H "Content-Type: application/json" -d "$rr" \
      "$KC/admin/realms/$realm/users/$said/role-mappings/realm" >/dev/null
    echo "      + realm role: $role"
  done
done

echo "==> done. Realms:"
for realm in "${REALMS[@]}"; do
  printf '   %-14s clients=%s\n' "$realm" \
    "$(curl -s "${auth[@]}" "$KC/admin/realms/$realm/clients" | python -c "import sys,json;print(','.join(sorted(c['clientId'] for c in json.load(sys.stdin) if not c['clientId'].startswith(('account','broker','realm-management','security-admin'))) ))")"
done
