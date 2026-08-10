#!/usr/bin/env bash
# Unit + contract test runner (no Docker required). CI (.github/workflows/ci.yml) runs the whole
# solution; this helper runs the fast, dependency-free suites per service and aggregates the result.
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

# --- Unit tests ---
run "Candidates.UnitTests"    "src/Services/Candidates/tests/Illumin360.Candidates.UnitTests/Illumin360.Candidates.UnitTests.csproj"
run "Students.UnitTests"      "src/Services/Students/tests/Illumin360.Students.UnitTests/Illumin360.Students.UnitTests.csproj"
run "Professionals.UnitTests" "src/Services/Professionals/tests/Illumin360.Professionals.UnitTests/Illumin360.Professionals.UnitTests.csproj"
run "Recruitment.UnitTests"   "src/Services/Recruitment/tests/Illumin360.Recruitment.UnitTests/Illumin360.Recruitment.UnitTests.csproj"

# --- Contract tests ---
run "Candidates.ContractTests" "src/Services/Candidates/tests/Illumin360.Candidates.ContractTests/Illumin360.Candidates.ContractTests.csproj"

echo "RESULT runtests=$RC"
exit $RC
