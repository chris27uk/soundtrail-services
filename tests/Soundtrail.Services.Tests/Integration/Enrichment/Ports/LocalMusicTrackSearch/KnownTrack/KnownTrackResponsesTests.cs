using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.LocalMusicTrackSearch.KnownTrack;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownTrackResponsesTests
{
    [Theory]
    [MemberData(nameof(LocalMusicTrackSearchContractModes.All), MemberType = typeof(LocalMusicTrackSearchContractModes))]
    public async Task Given_A_Known_Track_When_Local_Search_Runs_Then_The_Local_Metadata_Is_Returned(LocalMusicTrackSearchMode mode)
    {
        using var env = LocalMusicTrackSearchTestEnvironment.Create(mode);
        env.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "isrc-1",
            "mbid-1",
            123000,
            IsPlayable: false));

        var actual = await env.Search.GetByMusicCatalogIdAsync(MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        actual.Should().Be(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "isrc-1",
            "mbid-1",
            123000,
            IsPlayable: false));
    }
}
