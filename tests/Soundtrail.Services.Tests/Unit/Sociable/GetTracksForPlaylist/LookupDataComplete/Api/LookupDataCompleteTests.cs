using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataComplete.Api;

public sealed class LookupDataCompleteTests
{
    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Playlist_Id_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.PlaylistId.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Four_Tracks_Are_Returned()
    {
        var tracks = AllPlaylistTracks();
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(tracks);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks.Should().HaveCount(tracks.Length);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_First_Title_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].Title.Should().Be("Midnight Signals");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Track_Id_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));
        var expected = TrackId.TryCreate("Aurora Lane", "Midnight Signals", "Midnight Signals", new DateOnly(2023, 11, 10), null) switch
        {
            TrackIdCreateResult.Success success => success.Value,
            _ => throw new InvalidOperationException("The fixed scenario track should have a valid id.")
        };

        response!.Tracks[0].TrackId.Should().Be(expected);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Third_Title_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(GlassCities());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].Title.Should().Be("Glass Cities (Radio Edit)");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artist_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].ArtistName.Should().Be("Aurora Lane");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Album_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].AlbumTitle.Should().Be("Midnight Signals");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Duration_Is_Returned()
    {
        const int durationMs = 219876;
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(
            LookupDataCompleteTrack.MatchingCatalogTrack(
                "Duration Artist", "Duration Title", "Duration Artist", "Duration Title", "Duration Album",
                new DateOnly(2025, 6, 7), null, durationMs, default));

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].DurationMs.Should().Be(durationMs);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2025, 7, 8);
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(
            LookupDataCompleteTrack.MatchingCatalogTrack(
                "Release Artist", "Release Title", "Release Artist", "Release Title", "Release Album",
                releaseDate, null, 180000, default));

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Isrc_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].Isrc.Should().BeNull();
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artwork_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].ArtworkUrl.Should().BeNull();
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_First_Track_Is_Playable()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].Playable.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_First_Track_Has_One_Streaming_Location()
    {
        var track = MidnightSignals();
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(track);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].StreamingLocations.Should().HaveCount(track.StreamingLocations.Count);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Two_Tracks_Are_Playable()
    {
        var tracks = AllPlaylistTracks();
        var expectedPlayableTracks = tracks.Count(track => track.StreamingLocations.Count > 0);
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(tracks);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks.Count(track => track.Playable).Should().Be(expectedPlayableTracks);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Spotify_Url_Is_Returned()
    {
        const string spotifyUrl = "https://open.spotify.com/track/midnight-signals-test-input";
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(
            LookupDataCompleteTrackScenarios.MidnightSignals(default, spotifyUrl));

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].StreamingLocations[0].Url.Should().Be(spotifyUrl);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_YouTube_Music_Url_Is_Returned()
    {
        const string youtubeMusicUrl = "https://music.youtube.com/watch?v=glass-cities-test-input";
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(
            LookupDataCompleteTrackScenarios.GlassCities(default, youtubeMusicUrl));

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks[0].StreamingLocations[0].Url.Should().Be(youtubeMusicUrl);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Is_Completed()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Has_No_Next_Eligible_Time()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.NextEligibleAt.Should().BeNull();
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Has_No_Earliest_Completion_Time()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.EarliestExpectedCompletionAt.Should().BeNull();
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Reason_Is_Returned()
    {
        var environment = await GetTracksForPlaylistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.Reason.Should().Be("Lookup completed.");
    }
    private static LookupDataCompleteTrack MidnightSignals() =>
        LookupDataCompleteTrackScenarios.MidnightSignals(
            default,
            "https://open.spotify.com/track/midnight-signals");

    private static LookupDataCompleteTrack GlassCities() =>
        LookupDataCompleteTrackScenarios.GlassCities(
            default,
            "https://music.youtube.com/watch?v=glass-cities-radio");

    private static LookupDataCompleteTrack[] AllPlaylistTracks() =>
    [
        MidnightSignals(),
        LookupDataCompleteTrackScenarios.StaticHearts(default),
        GlassCities(),
        LookupDataCompleteTrackScenarios.GoldenEcho(default)
    ];
}
