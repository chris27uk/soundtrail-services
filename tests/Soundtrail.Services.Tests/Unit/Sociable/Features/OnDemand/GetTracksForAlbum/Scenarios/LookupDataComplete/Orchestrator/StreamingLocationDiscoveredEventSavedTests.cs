using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Scenarios.LookupDataComplete.Orchestrator;

public sealed class StreamingLocationDiscoveredEventSavedTests
{
    [Fact]
    public async Task Then_The_Music_Catalog_Id_Is_A_Track()
    {
        var environment = ForTrackWithStreamingLocation();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).MusicCatalogId.Should().BeOfType<CatalogItemId.Track>();
    }

    [Fact]
    public async Task Then_The_Music_Catalog_Track_Id_Comes_From_The_Input()
    {
        var input = InputTrack();
        var expectedTrackId = ((CatalogItem.MusicTrack)input.CatalogEntry.Item).Track.TrackId.Value;
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(input);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).MusicCatalogId.AsTrack().Value.Should().Be(expectedTrackId);
    }

    [Fact]
    public async Task Then_The_Hierarchy_Artist_Id_Comes_From_The_Input()
    {
        const string artist = "Streaming Input Artist";
        var input = InputTrack(artist);
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(input);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).Hierarchy.ArtistId!.Value.Value.Should().Be(input.CatalogEntry.ArtistId.Value);
    }

    [Fact]
    public async Task Then_The_Hierarchy_Album_Id_Comes_From_The_Input()
    {
        var environment = ForTrackWithStreamingLocation();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).Hierarchy.AlbumId.Should().BeNull();
    }

    [Fact]
    public async Task Then_The_Provider_Comes_From_The_Input()
    {
        var environment = ForTrackWithStreamingLocation();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).Provider.Should().Be(ProviderName.Spotify);
    }

    [Fact]
    public async Task Then_The_External_Id_Comes_From_The_Input()
    {
        var environment = ForTrackWithStreamingLocation();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).ExternalId.Should().BeNull();
    }

    [Fact]
    public async Task Then_The_Url_Comes_From_The_Input()
    {
        const string url = "https://open.spotify.com/track/streaming-event-input";
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(InputTrack(streamingUrl: url));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).Url.AbsoluteUri.Should().Be(url);
    }

    [Fact]
    public async Task Then_The_Source_Provider_Is_Odesli()
    {
        var environment = ForTrackWithStreamingLocation();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).SourceProvider.Should().Be(LookupSource.Odesli);
    }

    [Fact]
    public async Task Then_The_Observed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 21, 0, TimeSpan.Zero);
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            InputTrack(catalogUpdatedAt: requestTime));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).ObservedAt.Should().Be(requestTime);
    }

    private static GetTracksForAlbumSociableTestEnvironment ForTrackWithStreamingLocation() =>
        GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(InputTrack());

    private static LookupDataCompleteAlbumTrack InputTrack(
        string artist = "Streaming Scenario Artist",
        string streamingUrl = "https://open.spotify.com/track/streaming-scenario",
        DateTimeOffset catalogUpdatedAt = default) =>
        LookupDataCompleteAlbumTrack.Create(
            LookupDataCompleteAlbumTrackScenarios.DefaultAlbumId,
            artist,
            "Streaming Catalog Title",
            "Streaming Album",
            new DateOnly(2025, 5, 6),
            null,
            150000,
            catalogUpdatedAt,
            streamingLocations: [(ProviderName.Spotify, streamingUrl)]);

    private static StreamingLocationDiscovered Event(GetTracksForAlbumSociableTestEnvironment environment) =>
        environment.SavedEvents<StreamingLocationDiscovered>().First(@event => @event.Provider == ProviderName.Spotify);
}
