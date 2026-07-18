using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnUnknownMusicDataRequested;

public sealed class UnknownMusicDataRequestsWorkTests
{
    [Fact]
    public async Task Given_No_Local_Candidates_When_Handling_Then_External_Search_Work_Is_Requested()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest(query: "u2", searchType: SearchType.Artist));

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Single().Target
            .Should().Be(Work.SearchExternally(new("u2", SearchType.Artist)));
    }

    [Fact]
    public async Task Given_A_Track_Candidate_When_Handling_Then_Track_Streaming_Location_Work_Is_Requested()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        environment.SearchForCandidates.ResultToReturn = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateTrackCandidates("track-123");
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest());

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Single().Target
            .Should().Be(Work.EnrichTrackStreamingLocation(TrackId.From("track-123")));
    }

    [Fact]
    public async Task Given_An_Artist_Candidate_When_Handling_Then_Album_And_Track_Discovery_Work_Are_Requested()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        environment.SearchForCandidates.ResultToReturn = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateArtistCandidates("artist-123");
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest());

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Select(x => x.Target).Should().BeEquivalentTo([
            Work.DiscoverArtistAlbums(ArtistId.From("artist-123")),
            Work.DiscoverArtistTracks(ArtistId.From("artist-123"))
        ]);
    }

    [Fact]
    public async Task Given_An_Album_Candidate_When_Handling_Then_Album_Track_Discovery_Work_Is_Requested()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        environment.SearchForCandidates.ResultToReturn = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateAlbumCandidates("artist-123", "album-123");
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest());

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Single().Target
            .Should().Be(Work.DiscoverAlbumTracks(AlbumId.From("artist-123", "album-123")));
    }

    [Fact]
    public async Task Given_A_Playlist_Candidate_When_Handling_Then_No_Work_Is_Requested()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        environment.SearchForCandidates.ResultToReturn = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreatePlaylistCandidates("road trip");
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest());

        environment.Repository.AppendedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_An_Unknown_Request_When_Handling_Then_Candidate_Search_Uses_The_Search_Target()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var request = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest(query: "massive attack", searchType: SearchType.Track);

        await subject.Handle(request);

        environment.SearchForCandidates.LastTarget.Should().Be(new EnrichmentTarget.SearchForUnknownCatalogItem(request.SearchCriteria));
    }
}
