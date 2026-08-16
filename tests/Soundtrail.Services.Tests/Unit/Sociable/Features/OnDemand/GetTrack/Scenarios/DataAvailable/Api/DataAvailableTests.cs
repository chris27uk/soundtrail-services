using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTrack;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTrack.Scenarios.DataAvailable.Api;

public sealed class DataAvailableTests
{
    [Fact]
    public async Task When_Requesting_Then_A_Track_Is_Returned()
    {
        var trackId = TestTrackIds.Create("track-201");
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(trackId: trackId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Track_Id_Is_Returned()
    {
        var trackId = TestTrackIds.Create("track-203");
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(trackId: trackId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.TrackId.Should().Be(trackId);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Title_Is_Returned()
    {
        var title = "Track 205";
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(
            response: GetTrackScenarioData.CreateResponse(title: title));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.Title.Should().Be(title);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Artist_Name_Is_Returned()
    {
        var artistName = "Artist 206";
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(
            response: GetTrackScenarioData.CreateResponse(artistName: artistName));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ArtistName.Should().Be(artistName);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Album_Title_Is_Returned()
    {
        var albumTitle = "Album 207";
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(
            response: GetTrackScenarioData.CreateResponse(albumTitle: albumTitle));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.AlbumTitle.Should().Be(albumTitle);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Duration_Is_Returned()
    {
        var durationMs = 208000;
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(
            response: GetTrackScenarioData.CreateResponse(durationMs: durationMs));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.DurationMs.Should().Be(durationMs);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Isrc_Is_Returned()
    {
        var isrc = "GBAYE2400209";
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(
            response: GetTrackScenarioData.CreateResponse(isrc: isrc));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.Isrc.Should().Be(isrc);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Release_Date_Is_Returned()
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(
            response: GetTrackScenarioData.CreateResponse(releaseDate: releaseDate));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ReleaseDate.Should().Be(releaseDate);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Artwork_Url_Is_Returned()
    {
        var artworkUrl = "https://cdn.soundtrail.test/tracks/track-210.jpg";
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(
            response: GetTrackScenarioData.CreateResponse(artworkUrl: artworkUrl));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ArtworkUrl.Should().Be(artworkUrl);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Requested_Track_Id_Is_Read()
    {
        var trackId = TestTrackIds.Create("track-202");
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(trackId: trackId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.Port.RequestedTrackIds.Single().Should().Be(trackId);
    }

    [Fact]
    public async Task When_Requesting_Then_No_Enrichment_Work_Is_Scheduled()
    {
        var trackId = TestTrackIds.Create("track-201");
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(trackId: trackId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task When_Requesting_Track_Without_Streaming_Locations_Then_High_Priority_Odesli_Is_Scheduled()
    {
        var trackId = TestTrackIds.Create("track-odesli-missing");
        var environment = GetTrackSociableTestEnvironment.ForDataAvailable(
            trackId: trackId,
            response: GetTrackScenarioData.CreateResponse(trackId: trackId, streamingLocations: []));

        await environment.HandleWithoutPumpAsync();

        var request = environment.SentMessages.OfType<RequestKnownMusicDataMessage>().Should().ContainSingle().Subject;
        request.Priority.Should().Be(LookupPriorityBand.High);
        request.Operation.Should().BeOfType<CatalogItemOperation.StreamingLocationForTrack>()
            .Which.Id.Should().Be(trackId);
    }
}
