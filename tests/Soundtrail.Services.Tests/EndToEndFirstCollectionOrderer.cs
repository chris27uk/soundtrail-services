using Xunit;
using Xunit.Abstractions;

[assembly: TestCollectionOrderer(
    "Soundtrail.Services.Tests.EndToEndFirstCollectionOrderer",
    "Soundtrail.Services.Tests")]

namespace Soundtrail.Services.Tests;

/// <summary>
/// Starts the E2E collection as soon as workers are available so its fixture await
/// overlaps the parallel unit/integration wave instead of queuing until the end.
/// </summary>
public sealed class EndToEndFirstCollectionOrderer : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        testCollections
            .OrderBy(static collection =>
                collection.DisplayName.Contains("EndToEndHostCollection", StringComparison.Ordinal)
                    ? 0
                    : 1)
            .ThenBy(static collection => collection.DisplayName, StringComparer.Ordinal);
}
