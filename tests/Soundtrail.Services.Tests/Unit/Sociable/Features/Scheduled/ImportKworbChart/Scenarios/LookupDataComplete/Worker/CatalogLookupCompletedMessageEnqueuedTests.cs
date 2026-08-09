using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.ImportKworbChart.Scenarios.LookupDataComplete.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task When_Triggered_Then_The_Spotify_Playlist_Lookup_Succeeds()
    {
        var environment = ForCompletedChartTrack();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task When_Triggered_Then_The_Result_Value_Is_Playlist_Track_References()
    {
        var environment = ForCompletedChartTrack();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        Result(environment).Value.Should().BeOfType<LookedUpData.PlaylistTrackReferences>();
    }

    [Fact]
    public async Task When_Triggered_Then_The_Result_Contains_The_Seeded_Chart_Tracks()
    {
        var inputTracks = new[]
        {
            ChartTrack("First Artist", "First Title"),
            ChartTrack("Second Artist", "Second Title")
        };
        var environment = ImportKworbChartSociableTestEnvironment.ForLookupDataComplete(
            new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero),
            inputTracks);

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        PlaylistTracks(environment).Values.Should().HaveCount(inputTracks.Length);
    }

    [Fact]
    public async Task When_Triggered_Then_The_Result_Track_Artist_Comes_From_The_Chart_Seed()
    {
        const string artist = "Kworb Chart Artist";
        var environment = ImportKworbChartSociableTestEnvironment.ForLookupDataComplete(
            new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero),
            ChartTrack(artist, "Kworb Chart Title"));

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        PlaylistTracks(environment).Values.Single().ArtistName.Value.Should().Be(artist);
    }

    [Fact]
    public async Task When_Triggered_Then_The_Result_Track_Title_Comes_From_The_Chart_Seed()
    {
        const string title = "Kworb Chart Title";
        var environment = ImportKworbChartSociableTestEnvironment.ForLookupDataComplete(
            new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero),
            ChartTrack("Kworb Chart Artist", title));

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        PlaylistTracks(environment).Values.Single().TrackTitle.Should().Be(title);
    }

    [Fact]
    public async Task When_Triggered_Then_The_Result_Stream_Id_Targets_The_Worldwide_Song_Chart()
    {
        var environment = ForCompletedChartTrack();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        Result(environment).Context.StreamId.StableValue.Should().Be(
            $"child_tracks_for_playlist:{PlaylistId.FromPlaylistName("WorldwideSongChart").Value}");
    }

    [Fact]
    public async Task When_Triggered_Then_The_Original_Command_Id_Is_The_Spotify_Playlist_Lookup()
    {
        var environment = ForCompletedChartTrack();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        Result(environment).Context.OriginalCommandId.Should().Be(SpotifyPlaylistLookup(environment).Id);
    }

    [Fact]
    public async Task When_Triggered_Then_The_Correlation_Id_Is_Preserved_From_The_Spotify_Playlist_Lookup()
    {
        var environment = ForCompletedChartTrack();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        Message(environment).CorrelationId.Should().Be(SpotifyPlaylistLookup(environment).CorrelationId);
    }

    private static ImportKworbChartSociableTestEnvironment ForCompletedChartTrack() =>
        ImportKworbChartSociableTestEnvironment.ForLookupDataComplete(
            new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero),
            ChartTrack("Scenario Artist", "Scenario Title"));

    private static LookupDataCompleteTrack ChartTrack(string artist, string title) =>
        LookupDataCompleteTrack.MatchingCatalogTrack(
            artist,
            title,
            artist,
            title,
            "Scenario Album",
            new DateOnly(2025, 4, 5),
            null,
            140000,
            default);

    private static LookupPlaylistTracksByProviderMessage SpotifyPlaylistLookup(
        ImportKworbChartSociableTestEnvironment environment) =>
        environment.SentMessages<LookupPlaylistTracksByProviderMessage>()
            .Single(message =>
                message.Provider == ProviderName.Spotify &&
                message.PlaylistId == environment.PlaylistId);

    private static CatalogLookupCompleted Message(ImportKworbChartSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message =>
                message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.PlaylistTrackReferences &&
                succeeded.Context.OriginalCommandId == SpotifyPlaylistLookup(environment).Id);

    private static LookupResult.Succeeded Result(ImportKworbChartSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.PlaylistTrackReferences PlaylistTracks(
        ImportKworbChartSociableTestEnvironment environment) =>
        (LookedUpData.PlaylistTrackReferences)Result(environment).Value;
}
