using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Unit.GetTracksForAlbum;

public sealed class AlbumTracksExistTests
{
    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Port_Response_Is_Returned()
    {
        var response = AlbumTracks.CreateResponse();
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Artist_Id_Is_Returned()
    {
        var albumId = AlbumId.From("artist-1403", "album-1503");
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(albumId: albumId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistId.Value.Should().Be("artist-1403");
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Album_Id_Is_Returned()
    {
        var albumId = AlbumId.From("artist-1404", "album-1504");
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(albumId: albumId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.AlbumId.Should().Be(albumId);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Album_Title_Is_Returned()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(albumTitle: "Album 1505"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.AlbumTitle.Should().Be("Album 1505");
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Tracks_Are_Returned()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Id_Is_Returned()
    {
        var trackId = global::Soundtrail.Services.Tests.TestTrackIds.Create("track-1603");
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(trackId: trackId));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].TrackId.Should().Be(trackId);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Music_Catalog_Id_Is_Returned()
    {
        var trackId = global::Soundtrail.Services.Tests.TestTrackIds.Create("track-1604");
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(trackId: trackId));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].MusicCatalogId.Should().Be(new CatalogItemId.Track(trackId));
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Title_Is_Returned()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(title: "Track 1605"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].Title.Should().Be("Track 1605");
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Artist_Name_Is_Returned()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(artistName: "Artist 1606"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ArtistName.Should().Be("Artist 1606");
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Duration_Is_Returned()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(durationMs: 208000));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].DurationMs.Should().Be(208000);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Isrc_Is_Returned()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(isrc: "GBAYE2401608"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].Isrc.Should().Be("GBAYE2401608");
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(releaseDate: releaseDate));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Artwork_Url_Is_Returned()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(
            response: AlbumTracks.CreateResponse(artworkUrl: "https://cdn.soundtrail.test/tracks/track-1610.jpg"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/track-1610.jpg");
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Requested_Album_Id_Is_Read()
    {
        var albumId = AlbumId.From("artist-1402", "album-1502");
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(albumId: albumId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedAlbumIds.Single().Should().Be(albumId);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_A_Search_Command_Is_Sent()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Search_Command_Filter_Is_Album_Based()
    {
        var albumId = AlbumId.From("artist-1408", "album-1508");
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks(albumId: albumId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Operation.Should().Be(new CatalogItemOperation.ChildTracksForAlbum(albumId));
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Search_Command_Has_High_Priority()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Search_Command_Trust_Level_Is_One_Hundred()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Search_Command_Risk_Score_Is_Zero()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Search_Command_Requested_At_Is_Set()
    {
        var environment = GetTracksForAlbumUnitTestEnvironment.ForExistingAlbumTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RequestedAt.Should().Be(environment.Clock.UtcNow);
    }
}
