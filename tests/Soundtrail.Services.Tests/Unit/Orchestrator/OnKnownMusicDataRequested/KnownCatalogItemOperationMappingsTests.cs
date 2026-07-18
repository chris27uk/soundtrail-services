using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnKnownMusicDataRequested;

public sealed class KnownCatalogItemOperationMappingsTests
{
    [Fact]
    public async Task Given_A_Known_Track_Request_When_Handling_Then_Track_Streaming_Location_Work_Is_Requested()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownTrackRequest(trackId: "track-123"));

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Single().Target
            .Should().Be(Work.EnrichTrackStreamingLocation(TrackId.From("track-123")));
    }

    [Fact]
    public async Task Given_A_Known_Album_Request_When_Handling_Then_Album_Track_Discovery_Work_Is_Requested()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownAlbumRequest(artistId: "artist-123", albumId: "album-123"));

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Single().Target
            .Should().Be(Work.DiscoverAlbumTracks(AlbumId.From("artist-123", "album-123")));
    }

    [Fact]
    public async Task Given_A_Known_Playlist_Request_When_Handling_Then_Playlist_Track_Discovery_Work_Is_Requested()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownPlaylistRequest(playlistName: "road trip"));

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Single().Target
            .Should().Be(Work.DiscoverPlaylistTracks(PlaylistId.FromPlaylistName("road trip")));
    }
}
