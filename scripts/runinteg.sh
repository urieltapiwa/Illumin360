#!/usr/bin/env bash
# Integration test runner. Requires a Docker daemon (Testcontainers spins up PostgreSQL).
# CI runs these via the whole-solution `dotnet test`; this helper runs them per service.
set -uo pipefail
cd "$(dirname "$0")/.."

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
