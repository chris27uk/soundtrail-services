using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchTrackingStore.ByCriteria;

public sealed class ByCriteriaResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchTrackingStoreContractModes.All), MemberType = typeof(CatalogSearchTrackingStoreContractModes))]
    public async Task Given_An_Upserted_Tracking_When_Loading_By_Criteria_Then_It_Is_Returned(CatalogSearchTrackingStoreMode mode)
    {
        using var env = CatalogSearchTrackingStoreTestEnvironment.Create(mode);
        var tracking = new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero));

        await env.Store.UpsertAsync(tracking, CancellationToken.None);
        var actual = await env.Store.FindByCriteriaAsync(tracking.Criteria, CancellationToken.None);

        actual.Should().BeEquivalentTo(tracking);
    }
}
