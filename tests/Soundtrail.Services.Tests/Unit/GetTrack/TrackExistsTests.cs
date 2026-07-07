using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.GetTrack;

public sealed class TrackExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Port_Response_Is_Returned()
    {
        var response = Tracks.CreateTrackResponse();
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Track_Id_Is_Returned()
    {
        var trackId = TrackId.From("track-203");
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(trackId: trackId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.TrackId.Should().Be(trackId);
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Music_Catalog_Id_Is_Returned()
    {
        var trackId = TrackId.From("track-204");
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(trackId: trackId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.MusicCatalogId.Should().Be(new MusicCatalogId.Track(trackId));
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Title_Is_Returned()
    {
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(
            response: Tracks.CreateTrackResponse(title: "Track 205"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Title.Should().Be("Track 205");
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Artist_Name_Is_Returned()
    {
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(
            response: Tracks.CreateTrackResponse(artistName: "Artist 206"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistName.Should().Be("Artist 206");
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Album_Title_Is_Returned()
    {
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(
            response: Tracks.CreateTrackResponse(albumTitle: "Album 207"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.AlbumTitle.Should().Be("Album 207");
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Duration_Is_Returned()
    {
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(
            response: Tracks.CreateTrackResponse(durationMs: 208000));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.DurationMs.Should().Be(208000);
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Isrc_Is_Returned()
    {
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(
            response: Tracks.CreateTrackResponse(isrc: "GBAYE2400209"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Isrc.Should().Be("GBAYE2400209");
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(
            response: Tracks.CreateTrackResponse(releaseDate: releaseDate));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Artwork_Url_Is_Returned()
    {
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(
            response: Tracks.CreateTrackResponse(artworkUrl: "https://cdn.soundtrail.test/tracks/track-210.jpg"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/track-210.jpg");
    }

    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Requested_Track_Id_Is_Read()
    {
        var trackId = TrackId.From("track-202");
        var environment = GetTrackUnitTestEnvironment.ForExistingTrack(trackId: trackId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedTrackIds.Single().Should().Be(trackId);
    }
}