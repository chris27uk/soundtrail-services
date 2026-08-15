using Xunit.Sdk;
using Xunit.v3;

namespace Soundtrail.Services.Tests;

public sealed class EndToEndFirstCollectionOrderer : ITestCollectionOrderer
{
    public IReadOnlyCollection<TTestCollection> OrderTestCollections<TTestCollection>(IReadOnlyCollection<TTestCollection> testCollections)
        where TTestCollection : ITestCollection =>
        testCollections
            .OrderBy(static collection => IsEndToEnd(collection) ? 0 : 1)
            .ThenBy(static collection => GetDisplayName(collection), StringComparer.Ordinal)
            .ToArray();

    private static string GetDisplayName(ITestCollection collection) =>
        collection is ITestCollectionMetadata metadata
            ? metadata.TestCollectionDisplayName ?? collection.GetType().Name
            : collection.GetType().Name;

    private static bool IsEndToEnd(ITestCollection collection)
    {
        if (collection is not ITestCollectionMetadata metadata)
        {
            return false;
        }

        return metadata.TestCollectionDisplayName?.Contains("EndToEnd", StringComparison.OrdinalIgnoreCase) == true
            || metadata.TestCollectionClassName?.Contains("EndToEnd", StringComparison.OrdinalIgnoreCase) == true;
    }
}
