using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.CatalogProjector.Features.ProjectionReplay;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenCatalogProjectionReplayResponsesTests
{
    [Fact]
    public async Task Given_Replayable_MusicTrack_Events_When_Projecting_Then_Catalog_Documents_Are_Materialized_And_Searchable()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtistDiscovered("mc_track_1", 2, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 3, "album_hot_fuss", "Hot Fuss"),
            ProviderResolved("mc_track_1", 4, ProviderName.Spotify, "spotify-1"));

        var track = await env.LoadTrackAsync("mc_track_1");
        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");
        var search = await env.SearchAsync("mr brightside", types: "track", playback: "spotify");

        track.Should().NotBeNull();
        track!.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
        track.Title.Should().Be("Mr. Brightside");
        track.AvailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.Spotify.Value);

        artist.Should().NotBeNull();
        artist!.Name.Should().Be("The Killers");
        artist.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        album.Should().NotBeNull();
        album!.ArtistId.Should().Be("artist_the_killers");
        album.Name.Should().Be("Hot Fuss");
        album.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        search.Results.Should().ContainSingle();
        search.Results[0].Id.Should().Be("mc_track_1");
        search.Results[0].PlayabilityStatus.Should().Be(PlayabilityStatus.Playable);
        search.Results[0].AvailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.Spotify);
    }

    [Fact]
    public async Task Given_Album_Linked_Before_Artist_Linked_When_Projecting_Then_The_Album_Hierarchy_Is_Repaired_When_Artist_Arrives()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            AlbumDiscovered("mc_track_1", 2, "album_hot_fuss", "Hot Fuss"),
            ArtistDiscovered("mc_track_1", 3, "artist_the_killers", "The Killers"));

        var album = await env.LoadAlbumAsync("album_hot_fuss");
        var track = await env.LoadTrackAsync("mc_track_1");

        album.Should().NotBeNull();
        album!.ArtistId.Should().Be("artist_the_killers");
        album.ArtistName.Should().Be("The Killers");

        track.Should().NotBeNull();
        track!.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
    }

    [Fact]
    public async Task Given_A_Duplicate_Stored_Event_When_Projecting_Then_It_Is_Ignored()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();
        var storedEvent = ProviderResolved("mc_track_1", 1, ProviderName.Spotify, "spotify-1");

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 0, "Mr. Brightside", "The Killers"),
            storedEvent,
            storedEvent);

        var track = await env.LoadTrackAsync("mc_track_1");

        track.Should().NotBeNull();
        track!.AvailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.Spotify.Value);
    }

    [Fact]
    public async Task Given_A_Provider_Failure_Event_When_Projecting_Then_The_Provider_Becomes_Terminally_Unavailable()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtistDiscovered("mc_track_1", 2, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 3, "album_hot_fuss", "Hot Fuss"),
            ProviderFailed("mc_track_1", 4, ProviderName.YoutubeMusic));

        var track = await env.LoadTrackAsync("mc_track_1");
        var search = await env.SearchAsync("mr brightside", types: "track");

        track.Should().NotBeNull();
        track!.TerminallyUnavailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.YoutubeMusic.Value);
        search.Results.Should().ContainSingle();
        search.Results[0].PlayabilityStatus.Should().Be(PlayabilityStatus.TerminallyUnavailable);
        search.Results[0].TerminallyUnavailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.YoutubeMusic);
    }

    private static MusicTrackStoredEventRecordDto MinimalTrackInfo(
        string musicCatalogId,
        int version,
        string title,
        string artist) =>
        new()
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId, version),
            MusicCatalogId = musicCatalogId,
            Version = version,
            EventType = nameof(TrackDiscovered),
            Data = System.Text.Json.JsonSerializer.Serialize(
                new TrackDiscoveredEventDataRecordDto(
                    title,
                    artist,
                    222000,
                    "USIR20400274",
                    "mbid-1",
                    ProviderName.MusicBrainz.Value,
                    new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero))),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };

    private static MusicTrackStoredEventRecordDto ArtistDiscovered(
        string musicCatalogId,
        int version,
        string artistId,
        string artistName) =>
        new()
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId, version),
            MusicCatalogId = musicCatalogId,
            Version = version,
            EventType = nameof(ArtistDiscovered),
            Data = System.Text.Json.JsonSerializer.Serialize(
                new ArtistDiscoveredEventDataRecordDto(
                    artistId,
                    artistName,
                    ProviderName.MusicBrainz.Value,
                    new DateTimeOffset(2026, 6, 15, 12, 1, 0, TimeSpan.Zero))),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 15, 12, 1, 0, TimeSpan.Zero)
        };

    private static MusicTrackStoredEventRecordDto AlbumDiscovered(
        string musicCatalogId,
        int version,
        string albumId,
        string albumName) =>
        new()
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId, version),
            MusicCatalogId = musicCatalogId,
            Version = version,
            EventType = nameof(AlbumDiscovered),
            Data = System.Text.Json.JsonSerializer.Serialize(
                new AlbumDiscoveredEventDataRecordDto(
                    albumId,
                    albumName,
                    ProviderName.MusicBrainz.Value,
                    new DateTimeOffset(2026, 6, 15, 12, 2, 0, TimeSpan.Zero))),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 15, 12, 2, 0, TimeSpan.Zero)
        };

    private static MusicTrackStoredEventRecordDto ProviderResolved(
        string musicCatalogId,
        int version,
        ProviderName provider,
        string externalId) =>
        new()
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId, version),
            MusicCatalogId = musicCatalogId,
            Version = version,
            EventType = nameof(ProviderReferenceDiscovered),
            Data = System.Text.Json.JsonSerializer.Serialize(
                new ProviderReferenceDiscoveredEventDataRecordDto(
                    provider.Value,
                    externalId,
                    $"https://example.com/{externalId}",
                    ProviderName.Odesli.Value,
                    new DateTimeOffset(2026, 6, 15, 12, 3, 0, TimeSpan.Zero))),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 15, 12, 3, 0, TimeSpan.Zero)
        };

    private static MusicTrackStoredEventRecordDto ProviderFailed(
        string musicCatalogId,
        int version,
        ProviderName provider) =>
        new()
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId, version),
            MusicCatalogId = musicCatalogId,
            Version = version,
            EventType = nameof(ProviderReferenceLookupFailed),
            Data = System.Text.Json.JsonSerializer.Serialize(
                new ProviderReferenceLookupFailedEventDataRecordDto(
                    provider.Value,
                    ProviderName.Odesli.Value,
                    new DateTimeOffset(2026, 6, 15, 12, 4, 0, TimeSpan.Zero))),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 15, 12, 4, 0, TimeSpan.Zero)
        };
}
