using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Scenarios.LookupDataComplete.Api;

public sealed class LookupDataCompleteTests
{
    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artist_Id_Is_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.ArtistId.Should().Be(ArtistId.From(environment.AlbumId.ArtistId));
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Album_Id_Is_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.AlbumId.Should().Be(environment.AlbumId);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Album_Title_Is_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.AlbumTitle.Should().Be("Midnight Signals");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Tracks_Are_Returned()
    {
        var tracks = new[] { MidnightSignals(), StaticHearts() };
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(tracks);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Should().HaveCount(tracks.Length);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Track_Title_Is_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").Title.Should().Be("Midnight Signals");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Track_Id_Is_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());
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
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").TrackId.Should().Be(expected);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Track_Artist_Name_Is_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").ArtistName.Should().Be("Aurora Lane");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Duration_Is_Returned()
    {
        var durationMs = 214000;
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").DurationMs.Should().Be(durationMs);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Isrc_Is_Returned()
    {
        var isrc = "GBAYE2301110";
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").Isrc.Should().Be(isrc);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2023, 11, 10);
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Artwork_Url_Is_Returned()
    {
        var artworkUrl = "https://cdn.soundtrail.test/tracks/midnight-signals.jpg";
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Single(track => track.Title == "Midnight Signals").ArtworkUrl.Should().Be(artworkUrl);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Is_Completed()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Discovery!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedLookup(MidnightSignals());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }

    private static LookupDataCompleteAlbumTrack MidnightSignals() =>
        LookupDataCompleteAlbumTrackScenarios.MidnightSignals(default);

    private static LookupDataCompleteAlbumTrack StaticHearts() =>
        LookupDataCompleteAlbumTrackScenarios.StaticHearts(default);
}
