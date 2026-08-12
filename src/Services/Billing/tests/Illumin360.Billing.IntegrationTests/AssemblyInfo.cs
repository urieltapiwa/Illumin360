using Xunit;

// Each integration test sets the process-global ConnectionStrings__billing env var and boots a
// WebApplicationFactory against its own Testcontainers PostgreSQL, so they must not run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
