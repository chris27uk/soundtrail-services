using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.LookupDataComplete.Api;

public sealed class LookupDataCompleteTests
{
    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artist_Id_Is_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.ArtistId.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artist_Name_Is_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.ArtistName.Value.Should().Be("Aurora Lane");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Tracks_Are_Returned()
    {
        var tracks = new[] { MidnightSignals(), StaticHearts() };
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(tracks);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Should().HaveCount(tracks.Length);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Track_Title_Is_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").Title.Should().Be("Midnight Signals");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Track_Id_Is_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());
        var expected = TrackId.TryCreate(
            "Aurora Lane",
            "Midnight Signals",
            "Midnight Signals",
            new DateOnly(2023, 11, 10),
            null) switch
        {
            TrackIdCreateResult.Success success => success.Value,
            _ => throw new InvalidOperationException("The fixed scenario track should have a valid id.")
        };

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").TrackId.Should().Be(expected);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Track_Artist_Name_Is_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").ArtistName.Should().Be("Aurora Lane");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Album_Title_Is_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").AlbumTitle.Should().Be("Midnight Signals");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Duration_Is_Returned()
    {
        var durationMs = 214000;
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").DurationMs.Should().Be(durationMs);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Isrc_Is_Returned()
    {
        var isrc = "GBAYE2301110";
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").Isrc.Should().Be(isrc);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2023, 11, 10);
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artwork_Url_Is_Returned()
    {
        var artworkUrl = "https://cdn.soundtrail.test/tracks/midnight-signals.jpg";
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").ArtworkUrl.Should().Be(artworkUrl);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Is_Completed()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Streaming_Location_Was_Discovered_When_Requesting_Then_The_Track_Is_Playable()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(
            MidnightSignalsWithStreaming());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").Playable.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Streaming_Location_Was_Discovered_When_Requesting_Then_The_Streaming_Location_Url_Is_Returned()
    {
        const string spotifyUrl = "https://open.spotify.com/track/midnight-signals";
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedLookup(
            MidnightSignalsWithStreaming(spotifyUrl));

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").StreamingLocations
            .Should().ContainSingle()
            .Which.Url.Should().Be(spotifyUrl);
    }

    private static LookupDataCompleteArtistTrack MidnightSignals() =>
        LookupDataCompleteArtistTrackScenarios.MidnightSignals(default);

    private static LookupDataCompleteArtistTrack MidnightSignalsWithStreaming(
        string spotifyUrl = "https://open.spotify.com/track/midnight-signals") =>
        LookupDataCompleteArtistTrackScenarios.MidnightSignals(default, spotifyUrl);

    private static LookupDataCompleteArtistTrack StaticHearts() =>
        LookupDataCompleteArtistTrackScenarios.StaticHearts(default);
}
