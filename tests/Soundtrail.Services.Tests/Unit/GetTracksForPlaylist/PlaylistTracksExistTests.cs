using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

public sealed class PlaylistTracksExistTests
{
    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Port_Response_Is_Returned()
    {
        var response = PlaylistTracks.CreateResponse();
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.PlaylistId.Should().Be(response.PlaylistId);
        result.Tracks.Should().BeEquivalentTo(response.Tracks);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_Discovery_Feedback_Is_Attached()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();
        environment.DiscoveryFeedbackPort.Response = new DiscoveryFeedbackResponse(
            "pending",
            LookupPriorityBand.High,
            environment.Clock.UtcNow.AddSeconds(15),
            environment.Clock.UtcNow.AddSeconds(75),
            "Playlist lookup queued.",
            environment.Clock.UtcNow);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Discovery.Should().Be(environment.DiscoveryFeedbackPort.Response);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_Before_Assessment_Feedback_When_Requesting_Then_Retry_Timing_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Discovery.Should().NotBeNull();
        result.Discovery!.Status.Should().Be("scheduled");
        result.Discovery.NextEligibleAt.Should().Be(environment.Clock.UtcNow.AddSeconds(15));
        result.Discovery.EarliestExpectedCompletionAt.Should().Be(environment.Clock.UtcNow.AddSeconds(75));
        result.Discovery.Reason.Should().Be("Playlist lookup queued.");
    }

    [Fact]
    public async Task Given_Streaming_Discovery_Completed_But_Playlist_Track_Is_Not_Playable_When_Requesting_Then_Projection_Catch_Up_Timing_Is_Returned()
    {
        var trackId = TestTrackIds.Create("playlist-track-projection-lag");
        var response = PlaylistTracks.CreateResponse(trackId: trackId);
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(response: response);
        environment.DiscoveryFeedbackPort.SetResponse(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForPlaylist(response.PlaylistId)),
            new DiscoveryFeedbackResponse(
                "completed",
                LookupPriorityBand.High,
                null,
                null,
                "Lookup completed.",
                environment.Clock.UtcNow.AddSeconds(-2)));
        environment.DiscoveryFeedbackPort.SetResponse(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.StreamingLocationForTrack(trackId)),
            new DiscoveryFeedbackResponse(
                "completed",
                LookupPriorityBand.High,
                null,
                null,
                "Lookup completed.",
                environment.Clock.UtcNow.AddSeconds(-1)));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Discovery.Should().NotBeNull();
        result.Discovery!.Status.Should().Be("scheduled");
        result.Discovery.NextEligibleAt.Should().Be(environment.Clock.UtcNow.AddSeconds(15));
        result.Discovery.EarliestExpectedCompletionAt.Should().Be(environment.Clock.UtcNow.AddSeconds(75));
        result.Discovery.Reason.Should().Be("Track streaming projection is still catching up.");
    }

    [Fact]
    public async Task Given_Playlist_Discovery_Completed_But_Projected_Playlist_Has_No_Tracks_When_Requesting_Then_Projection_Catch_Up_Timing_Is_Returned()
    {
        var playlistId = PlaylistTracks.DefaultPlaylistId;
        var response = new GetTracksForPlaylistResponse(playlistId, []);
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(response: response);
        environment.DiscoveryFeedbackPort.SetResponse(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForPlaylist(playlistId)),
            new DiscoveryFeedbackResponse(
                "completed",
                LookupPriorityBand.High,
                null,
                null,
                "Lookup completed.",
                environment.Clock.UtcNow.AddSeconds(-1)));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks.Should().BeEmpty();
        result.Discovery.Should().NotBeNull();
        result.Discovery!.Status.Should().Be("scheduled");
        result.Discovery.NextEligibleAt.Should().Be(environment.Clock.UtcNow.AddSeconds(15));
        result.Discovery.EarliestExpectedCompletionAt.Should().Be(environment.Clock.UtcNow.AddSeconds(75));
        result.Discovery.Reason.Should().Be("Playlist projection is still catching up.");
    }

    [Fact]
    public async Task Given_Streaming_Discovery_Attempt_Failed_For_Unplayable_Track_When_Requesting_Then_Playlist_Discovery_Can_Be_Completed()
    {
        var trackId = TestTrackIds.Create("playlist-track-no-streaming-match");
        var response = PlaylistTracks.CreateResponse(trackId: trackId);
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(response: response);
        var playlistDiscovery = new DiscoveryFeedbackResponse(
            "completed",
            LookupPriorityBand.High,
            null,
            null,
            "Lookup completed.",
            environment.Clock.UtcNow.AddSeconds(-2));
        environment.DiscoveryFeedbackPort.SetResponse(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForPlaylist(response.PlaylistId)),
            playlistDiscovery);
        environment.DiscoveryFeedbackPort.SetResponse(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.StreamingLocationForTrack(trackId)),
            new DiscoveryFeedbackResponse(
                "attempt-failed",
                LookupPriorityBand.High,
                null,
                null,
                "No streaming location found.",
                environment.Clock.UtcNow.AddSeconds(-1)));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks.Single().Playable.Should().BeFalse();
        result.Discovery.Should().Be(playlistDiscovery);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Playlist_Id_Is_Returned()
    {
        var playlistId = PlaylistId.FromPlaylistName("RoadTrip");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(playlistId: playlistId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.PlaylistId.Should().Be(playlistId);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Tracks_Are_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Id_Is_Returned()
    {
        var trackId = TestTrackIds.Create("track-3303");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(trackId: trackId));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].TrackId.Should().Be(trackId);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Title_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(title: "Track 3305"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].Title.Should().Be("Track 3305");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Artist_Name_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(artistName: "Artist 3306"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ArtistName.Should().Be("Artist 3306");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Album_Title_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(albumTitle: "Album 3307"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].AlbumTitle.Should().Be("Album 3307");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Duration_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(durationMs: 208000));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].DurationMs.Should().Be(208000);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Isrc_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(isrc: "GBAYE2403309"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].Isrc.Should().Be("GBAYE2403309");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(releaseDate: releaseDate));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Track_Artwork_Url_Is_Returned()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(
            response: PlaylistTracks.CreateResponse(artworkUrl: "https://cdn.soundtrail.test/tracks/track-3311.jpg"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/track-3311.jpg");
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Requested_Playlist_Id_Is_Read()
    {
        var playlistId = PlaylistId.FromPlaylistName("FocusedMix");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(playlistId: playlistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedPlaylistIds.Single().Should().Be(playlistId);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_A_Search_Command_Is_Sent()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Should().ContainSingle().Which.Should().BeOfType<RequestKnownMusicDataMessage>();
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Filter_Is_Playlist_Based()
    {
        var playlistId = PlaylistId.FromPlaylistName("WorkoutMix");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(playlistId: playlistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands
            .Single()
            .Should()
            .BeEquivalentTo(
                new RequestKnownMusicDataMessage(
                    new CatalogItemOperation.ChildTracksForPlaylist(playlistId),
                    LookupPriorityBand.High,
                    100,
                    0,
                    environment.Clock.UtcNow)
                {
                    CreatedAt = environment.Clock.UtcNow
                },
                options => options.Excluding(x => x.Id).Excluding(x => x.CorrelationId));
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Has_High_Priority()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        ((RequestKnownMusicDataMessage)environment.CommandBus.Commands.Single()).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Trust_Level_Is_One_Hundred()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        ((RequestKnownMusicDataMessage)environment.CommandBus.Commands.Single()).TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Risk_Score_Is_Zero()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        ((RequestKnownMusicDataMessage)environment.CommandBus.Commands.Single()).RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_The_Search_Command_Requested_At_Is_Set()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        var command = (RequestKnownMusicDataMessage)environment.CommandBus.Commands.Single();
        command.RequestedAt.Should().Be(environment.Clock.UtcNow);
        command.CreatedAt.Should().Be(environment.Clock.UtcNow);
    }
}
