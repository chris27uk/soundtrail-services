using Xunit;

// Unique Raven DBs isolate documents, but EmbeddedServer still thrashes under full
// collection-parallel Create/Delete. Keep the assembly serial until Raven load is capped.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
