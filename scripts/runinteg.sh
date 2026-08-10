#!/usr/bin/env bash
# Integration test runner. Requires a Docker daemon (Testcontainers spins up PostgreSQL).
# CI runs these via the whole-solution `dotnet test`; this helper runs them per service.
set -uo pipefail
cd "$(dirname "$0")/.."

# --- Preflight: fail fast with a clear message if Docker/Postgres isn't reachable from the host,
# instead of letting each test dump an Npgsql "read past end of stream" stack trace. -------------
preflight() {
  if ! command -v docker >/dev/null 2>&1; then
    echo "PREFLIGHT: docker CLI not found. Integration tests need a Docker daemon." >&2
    return 1
  fi
  if ! docker info >/dev/null 2>&1; then
    echo "PREFLIGHT: Docker daemon not reachable. Start Docker and retry." >&2
    return 1
  fi

  echo "PREFLIGHT: probing host -> container PostgreSQL connectivity..."
  local cid port ok=""
  cid=$(docker run -d --rm -p 127.0.0.1::5432 -e POSTGRES_PASSWORD=preflight postgres:17-alpine 2>/dev/null) || {
    echo "PREFLIGHT: could not start a probe postgres container." >&2
    return 1
  }
  # shellcheck disable=SC2064
  trap "docker rm -f '$cid' >/dev/null 2>&1" RETURN

  port=$(docker port "$cid" 5432/tcp 2>/dev/null | head -1 | sed 's/.*://')
  if [ -z "$port" ]; then
    echo "PREFLIGHT: no host port mapped for the probe container." >&2
    return 1
  fi

  # Wait for postgres to accept connections inside the container, then confirm the mapped host
  # port completes a real PostgreSQL handshake from here. A bare TCP connect is not enough: Docker
  # Desktop / WSL2 port-forwarding can accept the TCP handshake but reset mid-protocol, which is
  # exactly where Npgsql dies ("read past end of stream" during SSL negotiation). So we send the
  # 8-byte SSLRequest (length=8, code=80877103) and require the server's 'S'/'N' reply byte.
  for _ in $(seq 1 20); do
    docker exec "$cid" pg_isready -q 2>/dev/null || { sleep 1; continue; }
    if exec 3<>"/dev/tcp/127.0.0.1/$port" 2>/dev/null; then
      printf '\x00\x00\x00\x08\x04\xd2\x16\x2f' >&3 2>/dev/null
      reply=$(LC_ALL=C timeout 3 dd bs=1 count=1 <&3 2>/dev/null)
      exec 3>&- 3<&- 2>/dev/null || true
      case "$reply" in
        S|N) ok=1; break ;;
      esac
    fi
    sleep 1
  done

  if [ -z "$ok" ]; then
    echo "PREFLIGHT: PostgreSQL container is up but its mapped host port (127.0.0.1:$port) is not" >&2
    echo "           reachable. This is usually Docker Desktop / WSL2 port-forwarding on Windows." >&2
    echo "           Integration tests will fail here; run them in CI (ubuntu + docker:dind)." >&2
    return 1
  fi

  echo "PREFLIGHT: OK (postgres reachable at 127.0.0.1:$port)."
  return 0
}

if ! preflight; then
  echo "RESULT integ=skipped (preflight failed)"
  exit 2
fi

RC=0

run() {
  echo "===== $1 ====="
  dotnet test "$2" -v minimal --nologo
  local rc=$?
  [ $rc -ne 0 ] && RC=1
  return $rc
}

run "Candidates.IntegrationTests"    "src/Services/Candidates/tests/Illumin360.Candidates.IntegrationTests/Illumin360.Candidates.IntegrationTests.csproj"
run "Professionals.IntegrationTests" "src/Services/Professionals/tests/Illumin360.Professionals.IntegrationTests/Illumin360.Professionals.IntegrationTests.csproj"

echo "RESULT integ=$RC"
exit $RC
