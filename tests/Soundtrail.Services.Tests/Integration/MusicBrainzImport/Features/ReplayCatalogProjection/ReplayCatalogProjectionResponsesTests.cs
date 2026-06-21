using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Features.ReplayCatalogProjection;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class ReplayCatalogProjectionResponsesTests
{
    [Theory]
    [MemberData(nameof(ReplayCatalogProjectionModes.All), MemberType = typeof(ReplayCatalogProjectionModes))]
    public async Task Given_A_Stale_Catalog_Projection_When_Replay_All_Is_Run_Then_The_Projection_Is_Rebuilt_From_Stored_Events(
        ReplayCatalogProjectionMode mode)
    {
        await using var env = await ReplayCatalogProjectionTestEnvironment.CreateAsync(mode);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        var result = await env.Handler.Handle(
            new ReplayCatalogProjectionCommand(true, []),
            CancellationToken.None);

        result.ReplayedStreamCount.Should().Be(1);
        result.ReplayedEventCount.Should().Be(3);

        var track = await env.LoadTrackAsync(musicCatalogId);
        track.Should().NotBeNull();
        track!.Title.Should().Be("Mr. Brightside");
        track.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");

        var checkpointVersion = await env.LoadCheckpointVersionAsync(musicCatalogId);
        checkpointVersion.Should().Be(3);
    }
}
