using Xunit;

// Integration test classes each spin up their own PostgreSQL container and set a process-global
// ConnectionStrings__candidates environment variable; running them in parallel would let one class
// clobber another's connection string. Serialize the whole assembly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
