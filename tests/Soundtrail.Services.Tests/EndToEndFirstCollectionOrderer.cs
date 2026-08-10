using Xunit;
using Xunit.Abstractions;

// Parallel collections ignore strict ordering for wall-clock start; keep E2E early in the
// queue so it claims a worker in the first wave (fixture InitializeAsync overlaps others).
[assembly: TestCollectionOrderer(
    "Soundtrail.Services.Tests.EndToEndFirstCollectionOrderer",
    "Soundtrail.Services.Tests")]

namespace Soundtrail.Services.Tests;

public sealed class EndToEndFirstCollectionOrderer : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        testCollections
            .OrderBy(static collection => IsEndToEnd(collection) ? 0 : 1)
            .ThenBy(static collection => collection.DisplayName, StringComparer.Ordinal);

    private static bool IsEndToEnd(ITestCollection collection) =>
        collection.DisplayName.Contains("EndToEnd", StringComparison.OrdinalIgnoreCase)
        || collection.CollectionDefinition?.Name.Contains("EndToEnd", StringComparison.OrdinalIgnoreCase) == true;
}
