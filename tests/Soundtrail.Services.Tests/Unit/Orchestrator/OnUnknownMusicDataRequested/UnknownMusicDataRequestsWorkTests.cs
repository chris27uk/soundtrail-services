using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

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
        environment.SearchForCandidates.ResultToReturn = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateTrackCandidates(TestTrackIds.Value("track-123"));
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest());

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Single().Target
            .Should().Be(Work.EnrichTrackStreamingLocation(TestTrackIds.Create("track-123")));
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

    [Fact]
    public async Task Given_The_Search_Target_Has_Already_Been_Requested_With_The_Same_Priority_When_Handling_Then_No_New_Event_Is_Appended()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var target = Work.SearchExternally(new SearchCriteria("u2", SearchType.Artist));
        environment.Repository.SeedEvents =
        [
            new WorkRequested(target, LookupPriorityBand.High, 50, 5, new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero), CorrelationId.From("corr-old"))
        ];
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest(query: "u2", searchType: SearchType.Artist));

        environment.Repository.AppendedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_The_Search_Target_Has_Already_Been_Requested_At_A_Lower_Priority_When_Handling_Then_A_WorkPriorityRaised_Event_Is_Appended()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var target = Work.SearchExternally(new SearchCriteria("u2", SearchType.Artist));
        environment.Repository.SeedEvents =
        [
            new WorkRequested(target, LookupPriorityBand.Low, 50, 5, new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero), CorrelationId.From("corr-old"))
        ];
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest(query: "u2", searchType: SearchType.Artist, priority: LookupPriorityBand.High, trustLevel: 88, riskScore: 7, correlationId: "corr-new"));

        var raised = environment.Repository.AppendedEvents.Should().ContainSingle().Which.Should().BeOfType<WorkPriorityRaised>().Subject;
        raised.Target.Should().Be(target);
        raised.Priority.Should().Be(LookupPriorityBand.High);
        raised.TrustLevel.Should().Be(88);
        raised.RiskScore.Should().Be(7);
        raised.CorrelationId.Value.Should().Be("corr-new");
    }
}
