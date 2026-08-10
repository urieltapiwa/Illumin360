using Xunit;

// These integration tests each set the process-global ConnectionStrings__employers env var and boot a
// WebApplicationFactory against their own Testcontainers PostgreSQL, so they must not run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
