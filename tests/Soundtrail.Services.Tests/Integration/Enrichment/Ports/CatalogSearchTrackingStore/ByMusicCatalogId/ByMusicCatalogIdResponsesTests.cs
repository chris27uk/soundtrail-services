using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchTrackingStore.ByMusicCatalogId;

public sealed class ByMusicCatalogIdResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchTrackingStoreContractModes.All), MemberType = typeof(CatalogSearchTrackingStoreContractModes))]
    public async Task Given_Multiple_Trackings_For_The_Same_MusicCatalogId_When_Loading_By_MusicCatalogId_Then_All_Are_Returned(CatalogSearchTrackingStoreMode mode)
    {
        using var env = CatalogSearchTrackingStoreTestEnvironment.Create(mode);
        var first = new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero));
        var second = new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song live"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 1, 0, TimeSpan.Zero));

        await env.Store.UpsertAsync(first, CancellationToken.None);
        await env.Store.UpsertAsync(second, CancellationToken.None);

        var actual = await env.Store.GetByMusicCatalogIdAsync(MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        actual.Should().BeEquivalentTo([first, second]);
    }
}
