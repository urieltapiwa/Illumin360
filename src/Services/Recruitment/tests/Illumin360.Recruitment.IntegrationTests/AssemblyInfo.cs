using Xunit;

// Each integration test class spins up its own PostgreSQL container and sets a process-global
// ConnectionStrings__recruitment environment variable; running them in parallel would let one class
// clobber another's connection string. Serialize the whole assembly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
