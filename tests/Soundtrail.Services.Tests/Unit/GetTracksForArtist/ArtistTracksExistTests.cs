using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.GetTracksForArtist;

public sealed class ArtistTracksExistTests
{
    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Port_Response_Is_Returned()
    {
        var response = ArtistTracks.CreateResponse();
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(response: response);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Artist_Id_Is_Returned()
    {
        var artistId = ArtistId.From("artist-2303");
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(artistId: artistId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistId.Should().Be(artistId);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Artist_Name_Is_Returned()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(artistName: "Artist 2304"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.ArtistName.Value.Should().Be("Artist 2304");
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Tracks_Are_Returned()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Id_Is_Returned()
    {
        var trackId = TrackId.From("track-2403");
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(trackId: trackId));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].TrackId.Should().Be(trackId);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Music_Catalog_Id_Is_Returned()
    {
        var trackId = TrackId.From("track-2404");
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(trackId: trackId));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].MusicCatalogId.Should().Be(new CatalogItemId.Track(trackId));
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Title_Is_Returned()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(title: "Track 2405"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].Title.Should().Be("Track 2405");
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Artist_Name_Is_Returned()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(trackArtistName: "Artist 2406"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ArtistName.Should().Be("Artist 2406");
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Album_Title_Is_Returned()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(albumTitle: "Album 2407"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].AlbumTitle.Should().Be("Album 2407");
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Duration_Is_Returned()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(durationMs: 208000));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].DurationMs.Should().Be(208000);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Isrc_Is_Returned()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(isrc: "GBAYE2402409"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].Isrc.Should().Be("GBAYE2402409");
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(releaseDate: releaseDate));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Artwork_Url_Is_Returned()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(
            response: ArtistTracks.CreateResponse(artworkUrl: "https://cdn.soundtrail.test/tracks/track-2411.jpg"));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result!.Tracks[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/track-2411.jpg");
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-2302");
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(artistId: artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_A_Search_Command_Is_Sent()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Search_Command_Filter_Is_Artist_Based()
    {
        var artistId = ArtistId.From("artist-2308");
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks(artistId: artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Operation.Should().Be(new CatalogItemOperation.ChildTracksForArtist(artistId));
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Search_Command_Has_High_Priority()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Search_Command_Trust_Level_Is_One_Hundred()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Search_Command_Risk_Score_Is_Zero()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Search_Command_Requested_At_Is_Set()
    {
        var environment = GetTracksForArtistUnitTestEnvironment.ForExistingArtistTracks();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().RequestedAt.Should().Be(environment.Clock.UtcNow);
    }
}
