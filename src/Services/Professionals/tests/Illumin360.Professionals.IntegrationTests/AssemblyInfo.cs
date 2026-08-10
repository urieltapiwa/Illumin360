using Xunit;

// Test classes here each spin up their own containers and set process-global environment variables
// (connection string, storage endpoint); running them in parallel would let one clobber another.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
