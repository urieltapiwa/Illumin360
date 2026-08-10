#!/usr/bin/env bash
# Full-suite runner. Builds once, then runs every test project SEQUENTIALLY. Integration suites each
# spin Testcontainers (PostgreSQL / MinIO); the solution-level `dotnet test` runs assemblies in parallel,
# which can saturate the Docker host (especially with the dev stack up) and cause flaky container
# timeouts. Running one project at a time keeps only one suite's containers alive at a time.
#
# Tip: for the fastest, most reliable local run, stop the dev stack first (docker compose down).
set -uo pipefail
cd "$(dirname "$0")/.."

echo "===== build ====="
dotnet build src/Illumin360.sln -v minimal --nologo || exit 1

RC=0
run() {
  echo "===== $1 ====="
  dotnet test "$2" --no-build -v minimal --nologo
  [ $? -ne 0 ] && RC=1
  return 0
}

# --- Unit + contract suites (no Docker) ---
UNIT=(
  "src/BuildingBlocks/Matching/tests/Illumin360.Matching.UnitTests/Illumin360.Matching.UnitTests.csproj"
  "src/BuildingBlocks/Resume/tests/Illumin360.Resume.UnitTests/Illumin360.Resume.UnitTests.csproj"
  "src/BuildingBlocks/Email/tests/Illumin360.Email.UnitTests/Illumin360.Email.UnitTests.csproj"
  "src/Services/Candidates/tests/Illumin360.Candidates.UnitTests/Illumin360.Candidates.UnitTests.csproj"
  "src/Services/Candidates/tests/Illumin360.Candidates.ContractTests/Illumin360.Candidates.ContractTests.csproj"
  "src/Services/Recruitment/tests/Illumin360.Recruitment.UnitTests/Illumin360.Recruitment.UnitTests.csproj"
  "src/Services/Students/tests/Illumin360.Students.UnitTests/Illumin360.Students.UnitTests.csproj"
  "src/Services/Professionals/tests/Illumin360.Professionals.UnitTests/Illumin360.Professionals.UnitTests.csproj"
  "src/Services/Admin/tests/Illumin360.Admin.UnitTests/Illumin360.Admin.UnitTests.csproj"
  "src/Services/Employers/tests/Illumin360.Employers.UnitTests/Illumin360.Employers.UnitTests.csproj"
)

# --- Integration suites (Docker; run strictly one at a time) ---
INTEG=(
  "src/BuildingBlocks/Email/tests/Illumin360.Email.IntegrationTests/Illumin360.Email.IntegrationTests.csproj"
  "src/Services/Candidates/tests/Illumin360.Candidates.IntegrationTests/Illumin360.Candidates.IntegrationTests.csproj"
  "src/Services/Students/tests/Illumin360.Students.IntegrationTests/Illumin360.Students.IntegrationTests.csproj"
  "src/Services/Professionals/tests/Illumin360.Professionals.IntegrationTests/Illumin360.Professionals.IntegrationTests.csproj"
  "src/Services/Employers/tests/Illumin360.Employers.IntegrationTests/Illumin360.Employers.IntegrationTests.csproj"
)

for p in "${UNIT[@]}"; do run "$(basename "$p" .csproj)" "$p"; done
echo "===== integration (serialized) ====="
for p in "${INTEG[@]}"; do run "$(basename "$p" .csproj)" "$p"; done

echo "RESULT runtests-all=$RC"
exit $RC
